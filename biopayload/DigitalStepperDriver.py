'''
===============================================================================
Program Description
	Defines a class for creating stepper motors driven by the Digital Stepper
    driver

Author:         Lucas Buening, lucas.r.buening@gmail.com
Maintainer:     Lucas Buening, lucas.r.buening@gmail.com
Version:        April 13, 2022
Status:         In progress
===============================================================================
'''
# External imports
from time import sleep

# Local imports
from interfaces import PinOutput, StepperMotor
from RaspiGpio import RpiPinOutput


class StepperMotorDSD(StepperMotor):
    '''Class for stepper motor driven by a Digital Stepper Driver'''

    def __init__(self, ENA: PinOutput, DIR: PinOutput, PUL: PinOutput, rpm: int = 200, microsteps: int = 400) -> None:
        '''Setup gpio pins and initialize parameters'''
        self.ENA = ENA
        self.DIR = DIR
        self.PUL = PUL

        self._pulseWidth = min((30 / (self.microsteps * rpm)), 3.0/1000000)
        self.microsteps = microsteps
        self.stepCount = 0

    def disable(self) -> None:
        '''Turn off the stepper motor'''
        self.ENA.set(0)

    def enable(self) -> None:
        '''Turn on the stepper motor'''
        self.ENA.set(1)

    def set_rpm(self, rpm: int) -> None:
        '''Calculate pulse width for desired rpm'''
        self._pulseWidth = min((30 / (self.microsteps * rpm)), 3.0/1000000)

    def set_direction(self, steps: int) -> None:
        '''Set the rotation direction'''
        if steps > 0:
            self.DIR.set(0)
        elif steps < 0:
            self.DIR.set(1)

    def _one_step(self) -> None:
        '''Take one step (step angle is based on microstepping setting)'''
        self.PUL.set(1)
        sleep(self._pulseWidth)
        self.PUL.set(0)
        sleep(self._pulseWidth)
        self.stepCount += 1

    def step(self, steps: int) -> None:
        '''Take a specified number of steps'''
        self.set_direction(steps)
        for _ in range(steps):
            self._one_step()


class DigitalStepperDriver():
    '''Class for creating stepper motors connected to the Digital Stepper Driver, driven through Raspberry Pi GPIO'''

    def __init__(self, ENA_pin: int, DIR_pin: int, PUL_pin: int) -> None:
        '''Save specified pin numbers for the enable, direction, and pulse pins'''
        self.ENA_pin = ENA_pin
        self.DIR_pin = DIR_pin
        self.PUL_pin = PUL_pin

    def get_stepper(self, rpm: int = 200, microsteps: int = 400) -> StepperMotorDSD:
        '''Create a StepperMotorDSD configured to use the pins that the Digital Stepper Driver is connected to'''
        ENA = RpiPinOutput(self.ENA_pin)
        DIR = RpiPinOutput(self.DIR_pin)
        PUL = RpiPinOutput(self.PUL_pin)
        return StepperMotorDSD(ENA, DIR, PUL, rpm, microsteps)
