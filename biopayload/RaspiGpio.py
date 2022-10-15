'''
===============================================================================
Program Description
	Defines an interface for driving GPIO pins on a Raspberry Pi 4B.

Author:         Lucas Buening, lucas.r.buening@gmail.com
Maintainer:     Lucas Buening, lucas.r.buening@gmail.com
Version:        April 13, 2022
Status:         In progress
===============================================================================
'''
# External Imports
import RPi.GPIO as GPIO
from interfaces import PinOutput, PinPWM

# Use Broadcom pin numbering as opposed to the board numbering
GPIO.setmode(GPIO.BCM)

class RpiPinOutput(PinOutput):
    '''Class for driving GPIO pins via RPi.GPIO'''

    def __init__(self, pin: int) -> None:
        GPIO.setup(pin, GPIO.OUT)
        self.pin = pin

    def set(self, value: int) -> None:
        '''Set output logic level of the pin to high or low'''
        GPIO.output(self.pin, GPIO.LOW if value == 0 else GPIO.HIGH)


class RpiPinPWM(PinPWM):
    '''Class for driving pwm pins via RPi.GPIO'''

    def __init__(self, pin: int, freq: int) -> None:
        GPIO.setup(pin, GPIO.OUT)
        self.pwm = GPIO.PWM(pin, freq)

    def start(self, duty_cycle: int) -> None:
        '''Start PWM on the pin with a specified duty cycle'''
        self.pwm.start(duty_cycle)

    def set_frequency(self, freq: int) -> None:
        '''Set PWM frequency'''
        self.pwm.ChangeFrequency(freq)

    def set_duty_cycle(self, duty_cycle: int) -> None:
        '''Set PWM duty cycle'''
        self.pwm.ChangeDutyCycle(duty_cycle)

    def stop(self) -> None:
        '''Stop PWM on the pin'''
        self.pwm.stop()
