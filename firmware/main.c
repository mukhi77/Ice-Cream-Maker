#include "driverlib.h"
#include <msp430fr5739.h>
#include <stdint.h>
#include <math.h>   // needed for logf

#define ADC_MAX        1023.0f
#define VREF           3.6f
#define RS             10000.0f

static const float R0     = 6759.7765f;     // ohms
static const float T0     = 309.35f;        // kelvin
static const float BETA   = 3510.3220f;
static const float KELVIN = 273.15f;


volatile uint8_t  g_sendStatus = 1;   // you can gate this with a command

typedef enum {
    ST_IDLE = 0,
    ST_PRECOOL = 1,
    ST_CHURN_RAMP = 2,
    ST_CHURN_CONTROL = 3,
    ST_ANTI_JAM = 4,
    ST_FINISH = 5,
    ST_FAULT = 6
} sm_state_t;

volatile sm_state_t g_state = ST_IDLE;
volatile uint8_t g_enabled = 0;      // set by UART 'S'/'X'
volatile uint8_t g_speedCmd = 0;     // 0..170
volatile int16_t  g_tempC_x100 = 0;  // temperature *100 for C#

// Temperature setpoints (C)
static const float T_PRECHURN = 25.0f; // To be calibrated => -2.0f;
static const float T_DONE     = 17.0f; // To be calibrated => -6.0f;

// Speeds (0..170)
#define SPD_OFF     0
#define SPD_PRECOOL 10 // 30
#define SPD_CHURN   70 // 90
#define SPD_FINISH  20 // 20

// Ramp timing at 10 Hz
#define RAMP_TICKS  50   // 5 seconds at 10 Hz

// Anti-jam pulse timing at 10 Hz
#define AJ_REV_TICKS    3   // 0.3 s
#define AJ_PAUSE_TICKS  2   // 0.2 s
#define AJ_FWD_TICKS    4   // 0.4 s

static uint8_t cnt_prechurn = 0;
static uint8_t cnt_done = 0;

#define SAT_INC(x, max) do { if ((x) < (max)) (x)++; } while(0)



// =======================
// CONFIG CONSTANTS
// =======================

#define MAX10BITS       1023
#define V_IN            3.6f
#define R_3             10000

// static const float R0     = 26170.0f;   // Ohms at T0
// static const float T0     = 274.35f;    // Kelvin
// static const float BETA   = 3332.617217f;
// static const float KELVIN = 273.15f;

#define TEMP_1          30
#define TEMP_2          20
#define TEMP_3          10
#define SPEED_1         10
#define SPEED_2         20
#define SPEED_3         30
#define SPEED_4         40

#define NUM_STATES      8

// PWM for phase drive (SMCLK assumed 1 MHz here)
#define PWM_PERIOD      400          // 1 MHz / 400 = 2.5 kHz PWM
#define PWM_ON          (PWM_PERIOD / 4)   // 25% duty

// Step timer mapping: f_step = (K_STEP * |v|), CCR0 = SMCLK / f_step
// With SMCLK = 1 MHz and K_STEP = 10 steps/s per unit of v (1..100):
//   f_step = 10 * |v|
//   CCR0   = 1e6 / (10 * |v|) = 100000 / |v|
#define STEP_K_NUM      100000UL     // numerator for CCR0 = STEP_K_NUM / |v|
#define STEP_CCR_MIN    500          // clamp for fastest speed (smaller CCR0 => faster)
#define STEP_CCR_MAX    60000        // clamp for slowest speed

// UART command buffer
#define VBUF_LEN   8

// =======================
// GLOBAL STATE
// =======================

volatile int8_t  stepIndex       = 0;       // 0..7
volatile int8_t  direction       = -1;       // +1 = CW, -1 = CCW
volatile uint8_t runContinuous   = 0;       // 0 = stopped, 1 = continuous run
volatile uint16_t stepPeriodTicks = 0;      // TA0CCR0 value

volatile char    vBuf[VBUF_LEN];
volatile uint8_t vIndex          = 0;
volatile uint8_t vCommandReady   = 0;

// Half-step table: bits 0..3 -> phases A,B,C,D
// A -> TB0.1 (P1.4), B -> TB0.2 (P1.5), C -> TB1.1 (P3.4), D -> TB1.2 (P3.5)
const uint8_t stepTable[NUM_STATES] = {
    (BIT0),                  // 1000
    (BIT0 | BIT1),           // 1100
    (BIT1),                  // 0100
    (BIT1 | BIT2),           // 0110
    (BIT2),                  // 0010
    (BIT2 | BIT3),           // 0011
    (BIT3),                  // 0001
    (BIT3 | BIT0)            // 1001
};

// =======================
// FUNCTION PROTOTYPES
// =======================

void init_clock(void);
void init_gpio(void);
void init_ADC(void);
void init_temp_timer(void);
void init_pwm_phases(void);
void init_step_timer(void);
void init_uart(void);

void uart_send(unsigned char data);

void stepper_apply_state(void);
void stepper_step_once(int8_t dir);
void process_velocity_command(void);
uint16_t compute_step_ccr0(uint8_t mag);

// UART data sender
void uart_send(unsigned char data)
{
    while (!(UCA0IFG & UCTXIFG));  // Wait until TX buffer ready
    UCA0TXBUF = data;
}

// =======================
// MAIN
// =======================

int main(void)
{
    WDTCTL = WDTPW | WDTHOLD;      // Stop watchdog

    init_clock();
    init_gpio();
    init_ADC();
    init_temp_timer();
    init_pwm_phases();
    init_step_timer();
    init_uart();

    __enable_interrupt();

    // Start with all phases de-energized
    stepIndex = 0;
    // stepper_apply_state();
    TB0CCR1 = 0;
    TB0CCR2 = 0;
    TB1CCR1 = 0;
    TB1CCR2 = 0;

    // --- Configure LED1 (PJ.0) ---
    PJDIR |= BIT0;   // output
    PJOUT &= ~BIT0;  // start off

    for (;;)
    {
        // Process any completed velocity command (from UART)
        process_velocity_command();

        // (Optional) enter low power mode here and wake on ISR if desired
        // __bis_SR_register(LPM0_bits | GIE);
        // __no_operation();
    }
}

// =======================
// INITIALIZATION
// =======================

void init_clock(void)
{
    // Set DCO to 8 MHz and ACLK = SMCLK = MCLK = DCO
    CSCTL0_H = CSKEY >> 8;          // Unlock CS registers
    CSCTL1 &= ~DCORSEL;
    CSCTL1 |= DCOFSEL_3;            // DCO = 8 MHz

    // ACLK = SMCLK = DCO
    CSCTL2 = SELA__DCOCLK | SELS__DCOCLK | SELM__DCOCLK;

    // Optional: divide ACLK or SMCLK if desired
    // Here: SMCLK = 1 MHz by dividing by 8 to simplify timer math
    CSCTL3 = DIVA__8 | DIVS__8 | DIVM__1;  // ACLK=1 MHz, SMCLK=1 MHz, MCLK=8 MHz

    CSCTL0_H = 0;                   // Lock CS registers
}

void init_gpio(void)
{
    // Disable high-impedance mode at power-on
    PM5CTL0 &= ~LOCKLPM5;

    // STEP PHASE OUTPUTS:
    // P1.4 -> TB0.1
    // P1.5 -> TB0.2
    P1DIR  |= BIT4 | BIT5;
    P1SEL0 |= BIT4 | BIT5;     // Timer function (check datasheet for exact SEL0/SEL1)
    P1SEL1 &= ~(BIT4 | BIT5);

    // P3.4 -> TB1.1
    // P3.5 -> TB1.2
    P3DIR  |= BIT4 | BIT5;
    P3SEL0 |= BIT4 | BIT5;     // Timer function
    P3SEL1 &= ~(BIT4 | BIT5);

    // ADC (thermistor) pin P2.7
    // P2DIR |= BIT7;  // set bit to select output
    // P2OUT |= BIT7; // drive P2.7 HIGH
    // P2SEL0 &= ~BIT7;  // clear primary function bit (p.78 of data sheet)
    // P2SEL1 &= ~BIT7 ;  // clear secondary function bit (p.78 of data sheet)
}

void init_ADC(void)
{
    // Set up ADC
        // Select ADC function for P1.0 (ADC channel A0)
    P1SEL0 |= BIT0;  // set bit
    P1SEL1 |= BIT0;  // set bit

        // Turn on ADC10_B
    ADC10CTL0 &= ~ADC10ENC;                         // Disable ADC before configuration
    ADC10CTL0 |= ADC10SHT_2 | ADC10ON;              // 16 x ADC10CLKs, turn on ADC
    ADC10CTL1 |= ADC10SHP;                          // Use sampling timer
    ADC10CTL2 |= ADC10RES;                          // 10-bit resolution
    ADC10MCTL0 |= ADC10SREF_0 | ADC10INCH_0;        // V+ = AVCC, V- = AVSS, start with A0
    ADC10CTL0 |= ADC10ENC;                          // Enable ADC
}

void init_temp_timer(void)
{
    // Timer_A1 CCR0 for 10Hz temp ISR
    TA1CTL = TASSEL__SMCLK | ID__8 | MC__UP | TACLR;
    TA1CCR0 = 12500 - 1; // 10Hz @ 1MHz SMCLK
    TA1CCTL0 = CCIE;
}

void init_pwm_phases(void)
{
    // Timer_B0: Phases A (TB0.1) and B (TB0.2)
    TB0CTL   = TBSSEL__SMCLK | MC__UP | TBCLR;   // SMCLK, up mode
    TB0CCR0  = PWM_PERIOD - 1;

    TB0CCTL1 = OUTMOD_7;   // Reset/Set
    TB0CCR1  = 0;          // Phase A initially OFF

    TB0CCTL2 = OUTMOD_7;
    TB0CCR2  = 0;          // Phase B initially OFF

    // Timer_B1: Phases C (TB1.1) and D (TB1.2)
    TB1CTL   = TBSSEL__SMCLK | MC__UP | TBCLR;   // SMCLK, up mode
    TB1CCR0  = PWM_PERIOD - 1;

    TB1CCTL1 = OUTMOD_7;
    TB1CCR1  = 0;          // Phase C OFF

    TB1CCTL2 = OUTMOD_7;
    TB1CCR2  = 0;          // Phase D OFF
}

void init_step_timer(void)
{
    // Timer_A0 used as step timer (generates step events)
    TA0CTL   = TASSEL__SMCLK | MC__UP | TACLR;   // SMCLK, up mode
    TA0CCR0  = 65535;                            // Large default; we'll overwrite later
    TA0CCTL0 = CCIE;                             // Enable CCR0 interrupt

    runContinuous   = 0;
    stepPeriodTicks = 0;
}

void init_uart(void)
{
    // --- UART pins (P2.0 TX, P2.1 RX) ---
    P2SEL0 &= ~(BIT0 | BIT1);
    P2SEL1 |=  (BIT0 | BIT1);
    UCA0CTLW0 |= UCSWRST;
    UCA0CTLW0 |= UCSSEL__SMCLK;
    UCA0BRW   = 6;
    UCA0MCTLW = UCOS16 | (8<<4) | (0x20 << 8);
    UCA0CTLW0 &= ~UCSWRST;
    UCA0IE |= UCRXIE;
}

// =======================
// STEPPER LOGIC
// =======================

void stepper_apply_state(void)
{
    uint8_t pattern = stepTable[stepIndex];

    // Phase A: TB0CCR1 (bit0)
    if (pattern & BIT0)
        TB0CCR1 = PWM_ON;     // Energize with 25% duty
    else
        TB0CCR1 = 0;          // De-energize (0% duty)

    // Phase B: TB0CCR2 (bit1)
    if (pattern & BIT2)
        TB0CCR2 = PWM_ON;
    else
        TB0CCR2 = 0;

    // Phase C: TB1CCR1 (bit2)
    if (pattern & BIT1)
        TB1CCR1 = PWM_ON;
    else
        TB1CCR1 = 0;

    // Phase D: TB1CCR2 (bit3)
    if (pattern & BIT3)
        TB1CCR2 = PWM_ON;
    else
        TB1CCR2 = 0;
}

void stepper_step_once(int8_t dir)
{
    stepIndex += dir;

    if (stepIndex >= NUM_STATES)
        stepIndex = 0;
    else if (stepIndex < 0)
        stepIndex = NUM_STATES - 1;

    stepper_apply_state();
}

uint16_t compute_step_ccr0(uint8_t mag)
{
    if (mag == 0) return 0;

    uint32_t ccr = STEP_K_NUM / mag;   // CCR0 = 100000 / |v|

    if (ccr < STEP_CCR_MIN) ccr = STEP_CCR_MIN;
    if (ccr > STEP_CCR_MAX) ccr = STEP_CCR_MAX;

    return (uint16_t)ccr;
}

// =======================
// VELOCITY COMMAND HANDLING
// =======================

void process_velocity_command(void)
{
    if (!vCommandReady)
        return;

    vCommandReady = 0;

    // Expected format: "V+NNN\n" or "V-NNN\n"
    if (vBuf[0] != 'V')
        return;

    char sign = vBuf[1];
    if ((sign != '+') && (sign != '-'))
        return;

    // Parse digits (simple and assumes valid digits)
    int d1 = vBuf[2] - '0';
    int d2 = vBuf[3] - '0';
    int d3 = vBuf[4] - '0';
    if (d1 < 0 || d1 > 9 || d2 < 0 || d2 > 9 || d3 < 0 || d3 > 9)
        return;

    uint8_t mag = (uint8_t)(d1 * 100 + d2 * 10 + d3);   // 0..999 (we'll use 0..100)

    if (mag == 0)
    {
        runContinuous = 0;
        stepPeriodTicks = 0;
        return;
    }

    if (mag > 170)
        mag = 170;

    direction = (sign == '-') ? -1 : +1;

    stepPeriodTicks = compute_step_ccr0(mag);
    TA0CCR0 = stepPeriodTicks;

    runContinuous = 1;
}

// =======================
// TEMPERATURE COMMAND HANDLING
// =======================

static float adc_to_tempC(uint16_t adc)
{
    // clamp
    if (adc == 0) adc = 1;
    if (adc >= 1023) adc = 1022;

    float v = ((float)adc / ADC_MAX) * VREF;
    float rth = (RS * v) / (VREF - v);

    float invT = (1.0f / T0) + (1.0f / BETA) * logf(rth / R0);
    float Tk = 1.0f / invT;
    return Tk - KELVIN;
}

static uint16_t read_adc_avg8(void)
{
    uint32_t sum = 0;
    uint8_t i;

    for (i = 0; i < 8; i++)
    {
        ADC10CTL0 |= ADC10SC;
        while (ADC10CTL1 & ADC10BUSY);
        sum += ADC10MEM0;
    }
    return (uint16_t)(sum / 8);
}


// =======================
// STATE MACHINE LOGIC
// =======================

// static uint8_t decide_speed_from_temp(float tempC)
// {
//     if (tempC >= T_HI) return 10;
//     if (tempC <= T_LO) return 100;
//     return 50;
// }

static void apply_speed(uint8_t speed)
{
    if (speed > 170) speed = 170;
    g_speedCmd = speed;

    if (speed == 0)
    {
        runContinuous = 0;
        stepPeriodTicks = 0;
        TB0CCR1 = TB0CCR2 = TB1CCR1 = TB1CCR2 = 0; // optional: de-energize
    }
    else
    {
        stepPeriodTicks = compute_step_ccr0(speed);
        TA0CCR0 = stepPeriodTicks;
        runContinuous = 1;
    }
}

static uint16_t stateTicks = 0;     // ticks since state entered (10 Hz)
static uint16_t rampTicks = 0;

static void set_state(sm_state_t s)
{
    g_state = s;
    stateTicks = 0;
    rampTicks = 0;

    // Reset “condition stability” buffers
    cnt_prechurn = 0;
    cnt_done = 0;
}

static void sm_update(float tempC)
{
    if (!g_enabled)
    {
        set_state(ST_IDLE);
        apply_speed(SPD_OFF);
        return;
    }

    switch (g_state)
    {
        case ST_IDLE:
            apply_speed(SPD_OFF);
            set_state(ST_PRECOOL);
            break;

        case ST_PRECOOL:
            apply_speed(SPD_PRECOOL);
            if (tempC <= T_PRECHURN)
                SAT_INC(cnt_prechurn, 15);   // 15 ticks @10Hz = 1.5s
            else
                cnt_prechurn = 0;

            if (cnt_prechurn >= 15)
            {
                cnt_prechurn = 0;
                set_state(ST_CHURN_RAMP);
            }
            break;

        case ST_CHURN_RAMP:
        {
            // linear ramp from PRECOOL -> CHURN over RAMP_TICKS
            uint16_t t = rampTicks;
            uint8_t spd;

            if (t >= RAMP_TICKS) t = RAMP_TICKS;
            spd = (uint8_t)(SPD_PRECOOL + (uint32_t)(SPD_CHURN - SPD_PRECOOL) * t / RAMP_TICKS);

            apply_speed(spd);

            if (rampTicks >= RAMP_TICKS)
                set_state(ST_CHURN_CONTROL);

            rampTicks++;
            break;
        }

        case ST_CHURN_CONTROL:
            apply_speed(SPD_CHURN);

            // Done condition: cold enough for 3 seconds
            if (tempC <= T_DONE)
                SAT_INC(cnt_done, 30);       // 30 ticks @10Hz = 3.0s
            else
                cnt_done = 0;

            if (cnt_done >= 30)
            {
                cnt_done = 0;
                set_state(ST_FINISH);
            }
            // Later: add current-based anti-jam trigger here
            break;

        case ST_ANTI_JAM:
            // Placeholder until current sensing is integrated
            set_state(ST_CHURN_CONTROL);
            break;

        case ST_FINISH:
            apply_speed(SPD_FINISH);
            if (stateTicks >= 50) // 5 seconds
            {
                g_enabled = 0;
                set_state(ST_IDLE);
                apply_speed(SPD_OFF);
            }
            break;

        case ST_FAULT:
        default:
            apply_speed(SPD_OFF);
            break;
    }

    stateTicks++;
}



// =======================
// INTERRUPTS
// =======================

// State variables for continuous command parsing
volatile uint8_t rx_state = 0;          // 0 = waiting for direction, 1 = waiting for magnitude
volatile int8_t velocity_direction = 0; // +1 or -1
volatile uint8_t velocity_magnitude = 0;

// UART RX ISR
#pragma vector=USCI_A0_VECTOR
__interrupt void USCI_A0_ISR(void)
{
        
    switch (__even_in_range(UCA0IV, 0x08))
    {
    case 0: break;                  // Vector 0 - no interrupt
    case 2:                         // Vector 2 - RXIFG
    {
        uint8_t ch = UCA0RXBUF;

        // Start/Stop/Reset commands
        if (ch == 'S') { g_enabled = 1; if (g_state == ST_IDLE) set_state(ST_PRECOOL); break; }
        if (ch == 'X') { g_enabled = 0; set_state(ST_IDLE); apply_speed(SPD_OFF); break; }
        if (ch == 'R') { if (g_state == ST_FAULT) { g_enabled = 0; set_state(ST_IDLE); } break; }

        // Optional direction set
        if (ch == '+') { direction = +1; break; }
        if (ch == '-') { direction = -1; break; }


        // if (ch == 'S') { 
        //     g_enabled = 1; 
        //     runContinuous = 1; 
        //     rx_state = 0; 
        //     break; 
        // }
        // if (ch == 'X') { 
        //     g_enabled = 0; 
        //     runContinuous = 0; 
        //     stepPeriodTicks = 0; 
        //     rx_state = 0; 
        //     break; 
        // }


        // Single-step commands
        if (ch == 'a') {            // CW single half-step
            runContinuous = 0;
            stepper_step_once(+1);
            break;
        }
        else if (ch == 'b') {       // CCW single half-step
            runContinuous = 0;
            stepper_step_once(-1);
            break;
        }

        if (rx_state == 0)
        {
            // Expecting direction byte
            if (ch == 43)      // '+'
            {
                velocity_direction = +1;
                rx_state = 1;
            }
            else if (ch == 45) // '-'
            {
                velocity_direction = -1;
                rx_state = 1;
            }
            else
            {
                // Invalid → reset parser
                rx_state = 0;
            }
        }
        else if (rx_state == 1)
        {
            // Expecting magnitude byte (0–100)
            velocity_magnitude = ch;
            if (velocity_magnitude > 170)
                velocity_magnitude = 170;

            if (velocity_magnitude == 0)
            {
                // Zero → stop motor
                runContinuous = 0;
            }
            else
            {
                // Valid speed → enable continuous motion
                direction = velocity_direction;

                // Compute CCR0 from speed magnitude
                stepPeriodTicks = compute_step_ccr0(velocity_magnitude);
                TA0CCR0 = stepPeriodTicks;

                runContinuous = 1;
            }

            // Done with packet → back to waiting for direction
            rx_state = 0;
        }

        break;
    }
    case 4: break;                  // Vector 4 - TXIFG (unused)
    default: break;
    }
}

// Step timer ISR (Timer_A0 CCR0)
#pragma vector=TIMER0_A0_VECTOR
__interrupt void TIMER0_A0_ISR(void)
{
    if (!runContinuous || stepPeriodTicks == 0)
        return;

    // Next step in the selected direction
    stepIndex += direction;
    if (stepIndex >= NUM_STATES)
        stepIndex = 0;
    else if (stepIndex < 0)
        stepIndex = NUM_STATES - 1;

    stepper_apply_state();
}



#pragma vector = TIMER1_A0_VECTOR
__interrupt void TIMER1_A0_ISR(void)
{
    static uint16_t adc_filt = 0;
    static uint8_t adc_init = 0;

    uint16_t adc;
    float tempC;

    // 1) sample and average
    adc = read_adc_avg8();

    // 2) EMA filter on ADC: 90/10
    if (!adc_init)
    {
        adc_filt = adc;
        adc_init = 1;
    }
    else
    {
        adc_filt = (uint16_t)((9U * adc_filt + 1U * adc) / 10U);
    }

    // 3) convert to temperature (you already have adc_to_tempC())
    tempC = adc_to_tempC(adc_filt);
    g_tempC_x100 = (int16_t)(tempC * 100.0f);

    // 4) update state machine
    sm_update(tempC);

    // 5) send status at 5 Hz
    {
        static uint8_t decim = 0;
        decim++;
        if (decim >= 2) // 10Hz/2 = 5Hz
        {
            decim = 0;
            uart_send(0xFF);
            uart_send((uint8_t)(g_tempC_x100 & 0xFF));
            uart_send((uint8_t)((g_tempC_x100 >> 8) & 0xFF));
            uart_send(g_speedCmd);
            uart_send((uint8_t)g_state);
        }
    }
}


// #pragma vector = TIMER1_A0_VECTOR
// __interrupt void TIMER1_A0_ISR(void)
// {
//     // 1) sample ADC
//     ADC10CTL0 |= ADC10SC;
//     while (ADC10CTL1 & ADC10BUSY);
//     uint16_t adc = ADC10MEM0;

//     // 2) compute temperature
//     float tempC = adc_to_tempC(adc);
//     g_tempC_x100 = (int16_t)(tempC * 100.0f);

//     // 3) decide speed (firmware owns this now)
//     uint8_t speed = decide_speed_from_temp(tempC);
//     g_speedCmd = speed;

//     // 4) apply to motor (reuse your existing speed->CCR0 logic)
//     if (speed == 0)
//     {
//         runContinuous = 0;
//         stepPeriodTicks = 0;
//     }
//     else
//     {
//         stepPeriodTicks = compute_step_ccr0(speed);
//         TA0CCR0 = stepPeriodTicks;
//         runContinuous = 1;
//         // direction can be fixed or set by last user command
//         // direction = +1;
//     }

//     // 5) optional: send status at low rate (e.g., 5 Hz if ISR is 100 Hz)
//     static uint8_t decim = 0;
//     decim++;
//     if (g_sendStatus && decim >= 20) // 100Hz/20 = 5Hz
//     {
//         decim = 0;
//         uart_send(0xFF);
//         uart_send((uint8_t)(g_tempC_x100 & 0xFF));        // low byte
//         uart_send((uint8_t)((g_tempC_x100 >> 8) & 0xFF)); // high byte
//         uart_send(g_speedCmd);
//         uart_send(g_state);
//     }

//     if (!g_enabled) { runContinuous = 0; g_speedCmd = 0; /* optionally return; */ }
// }

