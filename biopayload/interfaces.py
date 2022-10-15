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
from abc import ABC
from abc import abstractmethod
from typing import List

class PinOutput(ABC):
    '''Defines expected behavior of a GPIO output pin'''

    @abstractmethod
    def set(self, value: int) -> None:
        '''Set pin value to either 0 or 1'''
        ...


class PinPWM(ABC):
    '''Defines expected behavior of a PWM pin'''

    @abstractmethod
    def start(self, duty_cycle: int) -> None:
        '''Start PWM on the pin with a specified duty cycle'''
        ...

    @abstractmethod
    def set_frequency(self, freq: int) -> None:
        '''Set PWM frequency'''
        ...

    @abstractmethod
    def set_duty_cycle(self, duty_cycle: int) -> None:
        '''Set PWM duty cycle'''
        ...

    @abstractmethod
    def stop(self) -> None:
        '''Stop PWM on the pin'''
        ...


class DCMotor(ABC):
    '''Defines expected behavior of a DC motor'''

    @abstractmethod
    def start(self, speed: int):
        '''Turn on the motor'''
        ...

    @abstractmethod
    def set_direction(self, direction: int):
        '''Set direction of rotation of the motor, positive for counter-clockwise and negative for clockwise'''
        ...

    @abstractmethod
    def set_speed(self, speed: int):
        '''Set the speed of rotation of the motor'''
        ...

    @abstractmethod
    def stop(self):
        '''Turn off the motor'''
        ...


class StepperMotor(ABC):
    '''Defines expected behavior of a stepper motor'''

    @abstractmethod
    def disable(self) -> None:
        '''Turn off the stepper motor'''
        ...

    @abstractmethod
    def enable(self) -> None:
        '''Turn on the stepper motor'''
        ...

    @abstractmethod
    def set_rpm(self, rpm: int) -> None:
        '''Set the desired rpm'''
        ...

    @abstractmethod
    def set_direction(self, steps: int) -> None:
        '''Set the rotation direction'''
        ...

    @abstractmethod
    def step(self, steps: int) -> None:
        '''Take a specified number of steps'''
        ...


# Type alias for images represented as a 3d array (each 2d array is grayscale image)
Image = List[List[List[int]]]


class Camera(ABC):
    '''Defines expected behavior of a camera'''

    @abstractmethod
    def capture(self) -> Image:
        '''Take an image with the camera'''
        ...
