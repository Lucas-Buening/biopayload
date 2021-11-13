#!/usr/bin/env python3

"""
===============================================================================
Program Description
	Receives control messages from the central rover controller, and controls the
  motors, cameras, and other sensors of the Biopayload subcomponent.

Author:         Lucas Buening, lucas.r.buening@gmail.com
Maintainer:     Lucas Buening, lucas.r.buening@gmail.com
Version:        November 13, 2021
Status:         In progress
===============================================================================
"""

import rospy
from std_msgs.msg import Int8
import time
import atexit
import RPi.GPIO as GPIO

# Set GPIO modes
GPIO.setmode(GPIO.BCM)			# GPIO numbering
GPIO.setwarnings(False)			# enable warning from GPIO

class CytronMotorHat():
  """
  Class for setup and definitions of the Cytron Motor Hat
  """

  def __init__(self):
    # Configure motors
    self.M1 = self.motor(12, 24)    # M1 motor port uses GPIO pins 12(AN) and 24(DIG)
    self.M2 = self.motor(13, 26)    # M2 motor port uses GPIO pins 13(AN) and 26(DIG)

  class motor():
    """
    Class for a motor connected to the Cytron Motor Hat
    """

    def __init__(self, AN, DIG):
      self.AN = AN
      self.DIG = DIG

      # Configure pins as outputs
      GPIO.setup(self.AN, GPIO.OUT)
      GPIO.setup(self.DIG, GPIO.OUT)

      # Configure pwm
      time.sleep(1)				# delay for 1 seconds
      self.PWM = GPIO.PWM(self.AN, 100)

    def setSpeed(self, speed):
      # set DIG to control direction
      if speed > 0:
        GPIO.output(self.DIG, GPIO.LOW)
      elif speed < 0:
        GPIO.output(self.DIG, GPIO.HIGH)

      self.PWM.start(abs(speed))

class DigitalStepperDriver():
  """
  Class for setup and definitions of the Digital Stepper Driver
  """

  def __init__(self):
    # Setup for the Digital Stepper Driver
    self.ENA = 2         # set enable pin number
    self.DIR = 3         # set the direction pin number
    self.PUL = 4         # set the pulse pin number

    # Set all pins as output
    GPIO.setup(self.ENA, GPIO.OUT)
    GPIO.setup(self.DIR, GPIO.OUT)
    GPIO.setup(self.PUL, GPIO.OUT)

    # Set step count to 0
    self.stepCount = 0

  def disable(self):
    GPIO.output(self.ENA, GPIO.LOW)

  def enable(self):
    GPIO.output(self.ENA, GPIO.HIGH)

  def oneStep(self):
    GPIO.output(self.PUL, GPIO.HIGH)
    time.sleep(3.0/1000000)   # Sleep for 3 microseconds
    GPIO.output(self.PUL, GPIO.LOW)
    self.stepCount += 1

  def step(self, steps):
    # set DIR to control direction
      if steps > 0:
        GPIO.output(self.DIR, GPIO.LOW)
      elif steps < 0:
        GPIO.output(self.DIR, GPIO.HIGH)

      for i in range(steps):
        self.oneStep()

class Biopayload():
  """
  Class for controlling the biopayload components
  """

  def __init__(self):
      # Rename Cytron Port M1 and M2 as auger spin and auger vert
      CMH = CytronMotorHat()
      self.auger_spin = CMH.M1
      self.auger_vert = CMH.M2

      # Rename Digital Stepper Driver as carousel step
      self.carousel_step = DigitalStepperDriver()

      # Setup ROS node and topics
      rospy.init_node('biopayload_listener', anonymous=False)
      rospy.Subscriber('/auger_spin', Int8, self.augerSpin)
      rospy.Subscriber('/auger_vert', Int8, self.augerVert)
      rospy.Subscriber('/carousel_step', Int8, self.carouselStep)
      rospy.spin()
      
  def augerSpin(self, speed):
    """
    Function for setting the speed of the auger's DC motor
    """
    self.auger_spin.setSpeed(speed.data)

  def augerVert(self, speed):
    """
    Function for setting the speed of the auger's linear actuator
    """
    self.auger_vert.setSpeed(speed.data)

  def carouselStep(self, steps):
    """
    Function for moving the stepper motor
    """
    self.carousel_step.step(steps.data)

  @atexit.register
  def cleanup(self):
    self.auger_spin.setSpeed(0)
    self.auger_vert.setSpeed(0)
    self.carousel_step.disable()

if __name__ == '__main__':
  biopayload = Biopayload()
