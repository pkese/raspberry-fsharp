
# F# full-stack IoT starter project for Raspberry PI 

Raspberry Pi hosted [Suave](https://suave.io/) IoT app including [Fable](http://fable.io/) frontend providing browser access.  

Based on [SAFE-template](https://github.com/SAFE-Stack/SAFE-template) this project adds minor modifications for targeting IoT:

- IoT device with sensors will often need to push fresh data to all connected clients: all network traffic in both directions travels through [Websockets](https://github.com/ncthbrt/Fable.Websockets) instead of HTTP requests and responses.

- [Elmish](https://fable-elmish.github.io/elmish/) is used also on server side, because an IoT server is likely to be some sort of a state-machine. Having the same state machine abstraction (i.e. Elmish) on both sides makes things easier to reason about.

*This project intends to be **novice friendly** for F#/Fable/Elmish fans wishing to get effortlessly started with Raspberry Pi.*  
*It may however not not be as novice friendly to seasoned Raspberry Pi folks who are not yet familiar with F# and Elmish.*

## What will you need to get started

- Raspberry Pi 3 (works also on RPi-2 with a bit of custom config)
- Micro-SD card
- USB power adapter
- ...and either
  - access to TV or monitor with HDMI input and USB Keyboard (just to configure WiFi), or
  - Ethernet cable and free port on a switch with hacking capacity to figure out which IP was assigned to Raspberry by DHCP server.


# Raspberry PI installation: from zero to IoT in 20 minutes


Installation instructions below are focusing on installing DotNet on a Raspberry with Linux operating system.  

*Note: You could instead install a Raspberry Pi compatible IoT verison of Windows 10, which is covered [elsewhere](https://developer.microsoft.com/en-us/windows/iot/GetStarted).*

*Hint: There's just too much text on this page. When in hurry, feel free to skip anything written in italic - most of it is optional or contains ideas for situations when something goes wrong.*



## Put Linux on SD card

Download the right Linux distribution for your version of Raspberry Pi...

These instructions are for owners of Raspberry Pi 3, which is 64-bit, so we'll install a 64-bit operating system also known as `arm64` or `ARMv8`.

If you own a Raspberry Pi 2 you'll need to install a 32-bit `armhf` (aka `ARMv7`) OS instead. You can get one from [Debian](https://wiki.debian.org/RaspberryPi) or [Ubuntu](https://wiki.ubuntu.com/ARM/RaspberryPi), but in this case, your user credentials and WiFi/Timezone setup instructions might differ.

**Avoid Raspbian** based distributions. They may not work, because they are compiled for `armel`, which Raspbian did in order to be able to run on old Raspberry Pi version 1 built on `ARMv6` architecture. DotNet Core however requires `ARMv7` or `ARMv8`, so it will only run on a Raspberry v2 or v3 with an operating system built for either `armhf` or `arm64`.

*Notes:*  
*Raspberry Pi 2 v1.2 produced after October 2016 also contains 64-bit ARMv8 processor similar to RPi 3. You may need a WiFi dongle though.*  
*The `hf` in `armhf` stands for 'hardware floating point' which for 32-bit Arm became compulsory since ARMv7 (Raspberry Pi 2 and above).*  
*All Raspberry Pi Zeros are (as of late 2017) based on ARMv6, so they are not compatible.*

Some nice 64-bit Debian based images are available at https://github.com/bamarni/pi64/releases. Go ahead and download a recent `Lite` version.

Extract `.zip` archive and write the `.img` file to SD card.  

*Use [Rufus](https://rufus.akeo.ie/) or [Etcher](https://etcher.io/) (or just plain old `dd` if you are a proper Linux diehard) for copying and follow some [instructions](https://www.raspberrypi.org/documentation/installation/installing-images/) if necessary.*



## Boot for the first time and connect your Pi to the network


### a) When on WiFi

After inserting the SD card into Raspberry, connect your HDMI cable and USB keyboard and power it up.  
*Note: If the red LED turns off during boot, then your Pi is not getting enough juice and you should get a stronger USB power adapter (aim at least for 2 Amps; ideally 3).*

Once booted, login as user `pi` and type `raspberry` for password.

Start by running pi64-config to configure your timezone and connect to your Wifi network:

    sudo pi64-config

Login again after reboot and check if your internet connection is working

    ping 8.8.8.8

If it is, then use it to freshen your linux packages:

    sudo apt update
    sudo apt upgrade

Next, figure out what your local network address is so you can later access the Raspberry from your own computer:

    hostname -I

That's it. You can now plug the HDMI and keyboard back to your main computer. The rest will be done remotely.

### b) When on cable

Plug in an ethernet cable to your switch and figure out the IP address that DHCP server assigned to Raspberry (your router might be able to provide a list of connected DHCP clients or you can probe addresses assigned right next to your computer's).

## Got the IP address...

In my own case, the Pi's address was `192.168.11.90` so I'll just use this address from now on. You should however replace this address with your own whenever you see `192.168.11.90` (or just make a copy of this document and find-replace my address with yours, so you can then copy & paste directly without editing).



## Configure SSH

Try connecting to your Raspberry through ssh terminal from your own computer to see if connection can be established. Enter the same password as before: `raspberry`  
*Note: If you are lacking ssh client, make sure to install Open SSH. Restart and repoen your console terminal window after installing.*

    ssh pi@192.168.11.90

...then log-out.

You'll want to set up a password-less access to Raspberry by creating a pair of private and public security keys and then copying public key to Raspberry. To do that, run `ssh-keygen` on your computer which, will generate a pair of keys (in case you haven't done that already). Read the output of that command and see where it stored your keys in case you'll need to copy them manually.

To copy the public key to your Raspberry, run the following on you local machine:

    ssh-copy-id pi@192.168.11.90

If that didn't work, you can run:

    cat ~/.ssh/id_rsa.pub | ssh pi@192.168.11.90 "cat >> ~/.ssh/authorized_keys"

or on Windows:

    cd %USERPROFILE%\.ssh
    type id_rsa.pub | ssh pi@192.168.11.90 "cat >> ~/.ssh/authorized_keys"

*In case the above fails because `.ssh/` directory doesn't exist on Raspberry, you'll need to ssh there manually to create that directory using `mkdir ~/.ssh` then `chmod 700 ~/.ssh` and then try the above procedure again.*

Next, try if it worked by issuing `ssh pi@192.168.11.90`. This time Raspberry shouldn't ask you for password any more.

Ssh-ing into raspberry can be simplified even more by creating a file named `config` (just 'config' without any file extension) in `.ssh` directory on your computer and typing in:

    Host raspberry
        Hostname 192.168.11.90
        User pi
        ServerAliveInterval 60

After saving it, you can simply `ssh raspberry` instead of `ssh pi@192.168.11.90`



## Install DotNet Core

Unfortunately there isn't yet any SDK version of .Net for Linux/ARM, so we won't be able to do any software development and compilation on Linux. Instead we'll install just the .Net runtime (without SDK) on Raspberry Pi which will let us run only pre-compiled .dll files. We'll use our primary computer for the actual development. This turns out to be more efficient anyway, since PCs and Macs are much faster at compiling programs and more appropriate for hosting development environments.  
Then each time we'll build our program, we'll copy .dll files to Raspberry to run.

In addition, there isn't any 64-bit build of .Net for Arm either, so we'll have to install a 32-bit version.  
*IMHO: this should not be a bother, because 32-bit runtime is likely to consume less memory, which on Raspberry Pi is a limited resource, whereas for CPUs we still have 4 and F# makes it easy to use them all. :-)*

If you've installed a 64-bit linux on your Raspberry as explained above, you'll now have to configure support for installing 32-bit packages on what's otherwise a 64-bit system (skip this step if your Linux is 32-bit):

    sudo dpkg --add-architecture armhf
    sudo apt update

Next you'll need to install system dependencies for running .Net. In case of 64-bit Linux, run:

    sudo apt install libc6:armhf libc6-dev:armhf
    sudo apt install libunwind8:armhf liblttng-ust0:armhf libcurl3:armhf libuuid1:armhf libicu57:armhf
    sudo apt install libssl1.0.2:armhf libkrb5-3:armhf zlib1g:armhf

If your linux is 32-bit, then you can omit the `:armhf` suffix at each package:

    sudo apt install libc6 libc6-dev
    sudo apt install libunwind8 liblttng-ust0 libcurl3 libssl1.0.2 libuuid1 libkrb5-3 zlib1g libicu57

Finally we are ready to download and extract .Net:

    sudo apt install wget curl
    cd ~
    wget https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-linux-arm.tar.gz
    mkdir dotnet
    cd dotnet
    tar zxvf ../dotnet-runtime-latest-linux-arm.tar.gz
    ./dotnet --info

At this point, you have succeeded if you see something like:

    Microsoft .NET Core Shared Framework Host

        Version  : 2.0.4
        Build    : 7f262f453d8c8479b9af91d34c013b3aa05bc1ff


If anything goes wrong however, here are some useful links:  
https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x  
https://github.com/dotnet/announcements/issues/29  
https://github.com/dotnet/core-setup/blob/release/2.0.0/README.md#officially-released-builds  



## Running the F# app

Go to the directory where you cloned this GitHub repository and edit `build.fsx` file.  
Near the top of this file, you'll find:

    let raspberryHost = "192.168.11.90"

Replace that address with the address of your Raspberry. Depending if you're running windows or unix run either:

    build.cmd Publish
    ./build.sh Publish

If everything succeeded, you should see some output ending with `Smooth! Suave listener started in 520.29 with binding 0.0.0.0:8085`. Now navigate your browser to your own version of http://192.168.11.90:8085/ (remember to replace the address) and play with LEDs.

To change any functionality, edit:

    src/Server/App.fs
    src/Client/App.fs

After stopping the program (Ctrl-C unfortunately won't do the trick), you'll need to run

    ssh pi@192.168.11.90 "sudo killall dotnet"

If you wish to develop Web app separately and use the Fable's automatic reload feature, then open `src/Client/webpack.config.js` and replace `var proxyHost = "192.168.11.90"` with your own address. Then `cd` into `src/client`, run `dotnet fable webpack-dev-server` and navigate your browser to `http://localhost:8080`. Your browser should reload changes whenever you edit files, while the server side data is still coming from Raspberry.

On the other hand, when developing the Raspberry side of your app, you can speed up reloading by running `build Pi` which will do a minimal compile and upload just your app's DLL before starting the app. In case something goes wrong, or you install new Nuget packages, do a full `build Publish`.

## But before putting your IoT app up on the Internet...

...make sure to security-harden your Raspberry so that hackers won't break into it and use it for mining bitcoins or launching other attacks out from it.

The very least what you should consider is:
- change passwords for users `root` and `pi` (and ideally disable ssh password logins from public internet altogether),
- put your Raspberry behind the firewall and don't expose any port except web traffic to the outside traffic,
- add some form of authentication to your [Suave](https://suave.io/) app,
- `apt install nginx-light` and configure it as an HTTP proxy for your app,
- switch to `https` (e.g. install [Lets Encrypt](https://letsencrypt.org/) if you own a domain name),
- think about running your application as a normal user rather than `sudo` (or even user `pi` which also has `sudo` privilege),
- ...

-----------------
### Notes

1) When restarting the app, Suave may occasionally report `System.Net.Sockets.SocketException: Address already in use` even after dotnet has been killed. It takes maybe 30 seconds for port to release. This should be investigated (does anyone have any idea for a workaround?)

1) One thing after you get started with Raspberry, is to install Nuget packages to control Raspberry peripherials like Gpio ports, I2C bus, etc. Your mileage may vary - some of these lack support for recent Raspberry CPUs or 64-bit OS. Situation is likely to improve in near future. In worst case, there's always access to `/sys/bus/gpio`, `/sys/bus/i2c`, `/sys/bus/spi`, `/sys/bus/w1` etc.

1) Precise timings for peripherial electronics might be hard to achieve since neither Linux nor DotNet are real-time friendly. One option for a workaround is to launch a dedicated thread, set it to high priority and use tight timer loops inside it (never give it back to DotNet until it is done). If such I/O operations run only occasionally, it might be beneficial to also run a garbage collector just before starting an I/O interaction. Another option is to measure expected time that operation should take and compare it to real time taken - if they mismatch, it is likely that a context switch or garbage collection had happened in-between and that the process should be repeated. In reality this hardly ever occurs.

1) Unfortunately you won't be able to do any debugging of your Raspberry app just yet: remote debugging support for ARM is likely to appear with .Net Core version 2.1. However if you write your app using clean functional F# code, you shouldn't need a debugger anyway. ;-)

