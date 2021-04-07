from PIL import Image

# All pixels below the average plus the number of standard deviations are black, all above become white
def deviation_thresh(img, deviations):
    # Get dimensions of image and pixel list
    img_dims = img.size
    img_size = img_dims[0]*img_dims[1] #imgWidth*imgHeight
    pixel_list = list(img.getdata())

    # calculate average pixel value
    avg_pixel_val = sum(pixel_list)/img_size

    # calculate standard deviation
    std_dev = 0
    for pixel in pixel_list:
        std_dev += pow(pixel - avg_pixel_val, 2)
    std_dev = pow(std_dev / img_size, .5)

    # threshold using avg plus standard deviation times number of deviations asked for by user
    thresh = avg_pixel_val + (deviations * std_dev)
    white = 255
    black = 0
    threshed_img = [white if pixel>=thresh else black for pixel in pixel_list] #black and white tresholded img

    # A new image is formatted, given the data we manipulated, and then returned.
    new_img = Image.new(mode="L", size=img_dims) #black and white img
    new_img.putdata(threshed_img)

    return new_img

# Converts all pixels to their corresponding saturation values
def color_fil(img, RGB):
    # helper fn
    def _convertTo(RGBpixel, weights):
        maxChan = max(RGBpixel)  # max(R, G, B) finding which color channel is most saturated
        minChan = min(RGBpixel)  # min(R, G, B) finding which color channel is least saturated

        if maxChan > 0:
            sat = (maxChan - minChan) / maxChan  # saturation as a percentage
        else:  # corner case if all R,G,B channels are 0 meaning a pure black pixel
            sat = 0

        if int(sat * 255) < 128:
            return 0

        return (int((RGBpixel[0] * weights[0] + RGBpixel[1] * weights[1] + RGBpixel[2] * weights[2]) / 3))

    # Get dimensions of image and pixel list
    img_size = img.size  # size 2 tuple (img width, img height)
    img_list = list(img.getdata())  # size imgWidth*imgHeight 1d list. Each item in list is size 3 tuple (R, G, B)

    new_list = [_convertTo(RGBpixel, RGB) for RGBpixel in img_list]

    # A new image is formatted, given new values
    new_img = Image.new(mode="L", size=img_size)  # a black and white img
    new_img.putdata(new_list)
    return new_img


def color_check(in_path, out_path, RGB, deviations):

    img = Image.open(in_path)

    filtered = color_fil(img, RGB)
    threshed = deviation_thresh(filtered, deviations)

    threshed.save(out_path)

color_check("shinyblue.jpg", "ballblue.jpg", [0,0,1], 1)
color_check("beaker.jpg", "beakblue.jpg", [0,0,1], 1)
color_check("litmus.jpg", "litblue.jpg", [0,0,1], 4)
color_check("litmus.jpg", "litred.jpg", [2,0,-1], 1)


