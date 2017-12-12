

Save data found on 433 Mhz band

    /usr/local/bin/rtl_433 -g 20 -a -t -s 44100

Read temperatures

    vcgencmd measure_temp
    cat /sys/class/thermal/thermal_zone0/temp

Install vsdbg

    curl -sSL https://aka.ms/getvsdbgshbeta | bash /dev/stdin -v latest -r linux-arm -l ~/vsdbg

`etc/rc.local` (make sure to set executable flag on rc.local)

    echo "14"  > /sys/class/gpio/export
    echo "out" > /sys/class/gpio/gpio14/direction
    chgrp peter /sys/class/gpio/gpio14/value
    chmod g+w   /sys/class/gpio/gpio14/value

    chgrp peter /sys/class/leds/led?/brightness
    chmod g+w /sys/class/leds/led?/brightness