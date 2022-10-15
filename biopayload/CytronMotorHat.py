'''
===============================================================================
Program Description
	Defines an interface for using motors driven by the Cytron Motor Hat via 
    Raspberry Pi GPIO on a Raspberry Pi 4B.

Author:         Lucas Buening, lucas.r.buening@gmail.com
Maintainer:     Lucas Buening, lucas.r.buening@gmail.com
Version:        April 13, 2022
Status:         In progress
===============================================================================
'''
# Local Imports
from interfaces import PinOutput, PinPWM, DCMotor
from RaspiGpio import RpiPinOutput, RpiPinPWM

# Set the PWM frequency to drive the motors connected to the Cytron Motor Hat
CYTRON_PWM_FREQ = 100


class MotorPWM(DCMotor):
    '''Class for a DC motor driven by PWM'''

    def __init__(self, drive_pin: PinPWM, direction_pin: PinOutput, speed: int = 0) -> None:
        '''Set the pins used for driving and setting the direction of the motor'''
        self.drive_pin = drive_pin
        self.direction_pin = direction_pin
        self.speed = speed

    def start(self, speed: int = 0) -> None:
        '''Turn on the motor'''
        self.set_speed(speed)
        self.drive_pin.start(abs(self.speed))

    def set_direction(self, direction: int) -> None:
        '''Set the direction of rotation of the motor, positive for counter-clockwise and negative for clockwise'''
        if direction >= 0:
            self.direction_pin.set(0)
        self.direction_pin.set(1)

    def set_speed(self, speed: int) -> None:
        '''Set the speed of the motor'''
        self.set_direction(speed)

    def stop(self) -> None:
        '''Turn off the motor'''
        self.drive_pin.start(0)


class CytronMotorHat():
    '''Class for creating motors connected to the Cytron Motor Hat driven through Raspberry Pi GPIO'''

    def __init__(self):
        '''Create fields for the pin numbers of each motor port (from Cytron Motor Hat documentation)'''
        self._M1_AN = 12
        self._M1_DIG = 24
        self._M2_AN = 13
        self._M2_DIG = 26

    def _create_motor(self, AN: int, DIG: int) -> MotorPWM:
        '''Initialize a motor to use the specified pins'''
        return MotorPWM(RpiPinPWM(AN, CYTRON_PWM_FREQ), RpiPinOutput(DIG))

    def get_motor_M1(self) -> MotorPWM:
        '''Returns a motor object configured to use motor port 1'''
        return self._create_motor(self._M1_AN, self._M1_DIG)

    def get_motor_M2(self) -> MotorPWM:
        '''Returns a motor object configured to use motor port 2'''
        return self._create_motor(self._M2_AN, self._M2_DIG)
