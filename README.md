# PChouse Dynamic IPTables

This project is to be possible to set iptables rules for servers with traffic that the endpoints has dynamic dns (dynamic ip).  

Work as a service.

When service start a ipset hash:ip will be create for each rule and an iptables rule that match the ipset will be apply. Based on the schedule a request to dns for the ip will be made and the ipset set updated according with the ips gettered from the dns server.

When service stop all iptables and ipset will be removed.

To install clone this repository to your home and than run as root the install.sh that is located under the Install directory

To remove run as root the uninstall.sh

After install configurations are under /etc/dynamic-iptables

To start:
```bash
sudo systemctl start dynamic-iptables.service
```

To enable:
```bash
sudo systemctl enable dynamic-iptables.service
```

Configuration (/etc/dynamic-iptables/dynamic-iptables.conf)
```ini
[General]
; Configure the dynamic-iptables log
[Log]
; Log to console true, false
console=true
; Log to file file path or empty
file=/var/log/dynamic-iptables/dynamic-iptables.log
; Log level Verbose, Debug, Information, Warning, Error, Fatal
level=Verbose

[Commands]
iptables=/usr/sbin/iptables
ip6tables=/usr/sbin/ip6tables
ipset=/usr/sbin/ipset
```
Rule example under directory /etc/dynamic-iptables/conf.d/
```ini
; The rule name in ipset and iptables will be the same 
; has file name without .conf uppercase prefixed with DIP- and IPv as suffix
; e.g. DIP-RULE-IPV4
; Example configuration file for DynamicIPTables rule
[General]
; Type ACCEPT or REJECT or DROP
type=ACCEPT
; Destination port list comma separated, e.g. 80,443
ports=80,443
; Protocol list comma separated, e.g. tcp,udp
protocols=tcp
; ipv4 or ipv6 for both use both or empty
ipv=ipv4
;IPTABLES chain name. INPUT, OUTPUT
chain=OUTPUT
; Update interval in seconds, use the suffix:
; 'm' for minutes, e.g 5m for every 5 minutes; for minutes the minimum value is 5
; 'h' for hours, eg 1h for every 1 hour;
; 'H' for a fixed time of day. ex 19H09
interval=5m
; Timezone for the fixed time of day
timezone=Europe/Lisbon
; comma separated list of domains
domains=acme-v02.api.letsencrypt.org
```

## Credits
[João M F Rebelo](https://github.com/joaomfrebelo)

## License

MIT License

Copyright (c) 2024 Reflexão Estudos e Sistemas Informáticos, LDA (PChouse)

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.

