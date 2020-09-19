# HueShift 

Make your Phillips Hue lights cool during the day and warm after sunset just like Flux and Apple's Night Shift.  Runs comfortably in Docker on a Raspberry Pi.

Install on a Raspberry Pi, and your house rises and sets with the sun.  When a light gets turned on, it will automatically change to the right color within X seconds.  (Built on .NET Core so this will run on Windows and Linux as well!) 

### Science 

In order for your body to properly regulate your sleep cycle, you need to be exposed to blue light during the day and only red light at night.  This project is originally inspired from the awesome program, Flux, that I highly recommend installing on your machine right now https://justgetflux.com/.  (Flux can control Hue too, but it doesn't get the job done.) 

HueShift automatically geolocates against your IP address and continually calculates sunrise and sunset for your latitude and longitude.  During the daytime, your bulbs are forced to be as cool as possible.  After sunset, they are as red as possible. There's lots of configuration you can do through the command line or the generated configuration file.

## Instructions for Raspberry Pi:

Log in as the pi user:

Install Docker and grab the compose file.

```
sudo apt update
sudo apt install -y docker docker-compose
mkdir hueshift && cd hueshift
wget "https://raw.githubusercontent.com/akutruff/HueShift/master/docker-compose.yml"
```

edit ```docker-compose.yml``` file and change to the timezone that's best for you this [list](https://docs.diladele.com/docker/timezones.html)

```
docker-compose up -d
```

#### Hit the button on the Hue bridge!  

The code at the moment tries to connect to the bridge before the program times out.  It may spit out an exception saying it can't find the bridge.  That's okay.  Just hit the button on the front of the bridge.  It will try three times and then quit.  

If all seems okay, test it.  Change the color of your lights in the Hue app or via Alexa.  Wait 10 seconds.  Your lights should automagically shift to blu-ish during the day, and red-ish at night.  Try it a few times, the code is checking every 10 seconds so it may override your attempt at changing the colors. (That's the point afer all.)

Now put your Pi somewhere, and leave it on, laugh heartily, and you now have an automatic sunrise and sunset machine!  

### Customization:

`/home/pi/.config/hueShift-conf.json`

After you run HueShift the first time, a conf file will appear in the Hue directory.  It's pretty self explanatory if you crack it open you can edit the defaults and discovered values.  Make sure the service has been stopped before editing the file.  (It's quick and dirty code but stable.)

If you have an advanced setup with your Hue running on a different subnet or if UPnP / HTTP discovery aren't working, you can crack the conf file and manually enter your hostname for the bridge.  May need to specify port 80 as well if it's not working with just the IP.

### Others Work:
The experimental DMX group control code was lifted in entirety from ![ArtDot](https://github.com/cansik/ArtNet3DotNet/tree/master/ArtDotNet)
