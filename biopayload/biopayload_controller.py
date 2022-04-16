#!/usr/bin/env python3

'''
===============================================================================
Program Description
	Receives control messages from the central rover controller, and controls the
  motors, cameras, and other sensors of the Biopayload subcomponent.

Author:         Lucas Buening, lucas.r.buening@gmail.com
Maintainer:     Lucas Buening, lucas.r.buening@gmail.com
Version:        April 13, 2022
Status:         In progress
===============================================================================
'''


class Biopayload():
    '''
    Class for controlling the biopayload components
    '''

    def __init__(self):
        # Rename Cytron Port M1 and M2 as auger spin and auger vert
        CMH = CytronMotorHat()
        self.auger_spin = CMH.M1
        self.auger_vert = CMH.M2

        # Rename Digital Stepper Driver as carousel step
        self.carousel_step = DigitalStepperDriver()
        self.carousel_step.enable()
        self.carousel_step.setRPM(200)

        # Setup ROS node and topics
        rospy.init_node('biopayload_listener', anonymous=False)
        rospy.Subscriber('/auger_spin', Int8, self.augerSpin)
        rospy.Subscriber('/auger_vert', Int8, self.augerVert)
        rospy.Subscriber('/carousel_step', Int8, self.carouselStep)
        rospy.spin()

    def augerSpin(self, speed):
        '''
        Function for setting the speed of the auger's DC motor
        '''
        self.auger_spin.setSpeed(speed.data)

    def augerVert(self, speed):
        '''
        Function for setting the speed of the auger's linear actuator
        '''
        self.auger_vert.setSpeed(speed.data)

    def carouselStep(self, steps):
        '''
        Function for moving the stepper motor
        '''
        self.carousel_step.step(steps.data)


def main():
    biopayload = Biopayload()


if __name__ == '__main__':
    main()
