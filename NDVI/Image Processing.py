import numpy as np
import cv2
# ROS Image message
from sensor_msgs.msg import Image

def contrast_stretch(im):
    """
    Performs a simple contrast stretch of the given image, from 5-95%.
    """
    in_min = np.percentile(im, 5)
    in_max = np.percentile(im, 95)

    out_min = 0.0
    out_max = 255.0

    out = im - in_min
    out *= ((out_min - out_max) / (in_min - in_max))
    out += in_min

    return out


def importLUT(fileName):
    """
    Import a look up table from a tab delimited text file
    """
    rLUT = np.loadtxt(fileName, '\t', skiprows=1, usecols=1, unpack=True)
    rLUT = rLUT.astype(int)

    gLUT = np.loadtxt(fileName, '\t', skiprows=1, usecols=2, unpack=True)
    gLUT = gLUT.astype(int)

    bLUT = np.loadtxt(fileName, '\t', skiprows=1, usecols=3, unpack=True)
    bLUT = bLUT.astype(int)

    lut = np.dstack((bLUT, gLUT, rLUT))
    return lut


def calcNDVI(im):
    """
    Perform an NDVI calculation on the image data
    """

    # Get the individual colour components of the image
    b, g, r = cv2.split(im)

    # Bottom of fraction
    bottom = (r.astype(float) + b.astype(float))
    bottom[bottom == 0] = 0.01  # ensure that we don't divide by 0

    # Perform NDVI calculation
    ndvi = (r.astype(float) - b) / bottom

    # Increase constrast
    ndvi = contrast_stretch(ndvi)

    # Reformat and return the NDVI data
    ndvi = ndvi.astype(np.uint8)
    return ndvi


def applyLUT(im, lut):
    """
    Apply the look up table to the image
    """
    im = cv2.cvtColor(im, cv2.COLOR_GRAY2BGR)
    im = cv2.LUT(im, lut)
    im = im.astype(np.uint8)
    return im


def NDVITransform(im):
    """
    Apply the NDVI transformation to the image
    """
    # Calculate the NDVI
    ndvi = calcNDVI(im)

    # Apply the look up table to the image
    lut = importLUT("Colormap.txt")
    ndvi = applyLUT(ndvi, lut)

    return ndvi

def msgTransform(msg):

    try:
        # Convert your ROS Image message to OpenCV2
        cv2_img = bridge.imgmsg_to_cv2(msg, "bgr8")
    except CvBridgeError as e:
        print(e)

    # Save your OpenCV2 image as a jpeg
    cv2.imwrite('rock_RGB.jpeg', cv2_img)

    ndvi = NDDVITransform(cv2_img)
    cv2.imwrite('rock_NDVI.jpeg', cv2_img)

class Rock_Analysis:

    def __init__(self):
        rospy.init_node("rockimg_listener", anonymous=False)
        rospy.Subscriber('/rock_img', Image, msgTransform)

        #image = cv2.imread("Images\\blogger-image.jpg", 1)
        #NDVITransform(image)

if __name__ == '__main__':
    rock_analysis = Rock_Analysis