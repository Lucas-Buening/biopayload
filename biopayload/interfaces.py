'''
===============================================================================
Program Description
	Defines interfaces (abstract base classes or protocols) to faciliate 
	compatability between modules.

Author:         Lucas Buening, lucas.r.buening@gmail.com
Maintainer:     Lucas Buening, lucas.r.buening@gmail.com
Version:        April 13, 2022
Status:         In progress
===============================================================================
'''
# External Imports
from typing import Protocol


class PinOutput(Protocol):
    '''Defines expected behavior of a GPIO output pin'''

    def set(self, value: int) -> None:
        '''Set pin value to either 0 or 1'''


class PinPWM(Protocol):
    '''Defines the functions that are expected of a PWM pin'''

    def start(self, duty_cycle: int) -> None:
        '''Start PWM on the pin with a specified duty cycle'''

    def set_frequency(self, freq: int) -> None:
        '''Set PWM frequency'''

    def set_duty_cycle(self, duty_cycle: int) -> None:
        '''Set PWM duty cycle'''

    def stop(self) -> None:
        '''Stop PWM on the pin'''
