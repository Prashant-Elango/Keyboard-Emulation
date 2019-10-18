# Keyboard-Emulation
Use existing computer as a keyboard and Tunnel all host keyboard inputs to remote machine.

Note: CTRL-ALT-DEL combination will not be sent to remote machine.

Commands can be executed from console or powershell
# Commands

## Keyboard Server
### Default configuration
Launch keyboard server with default port and available internal IPV4 address.
```javascript
KeyboardServer
```
### Launch keyboard Server with different internal IPV4 address.
```javascript
KeyboardServer -c 192.168.1.2
```
### Launch keyboard server with different port.
```javascript
KeyboardServer -p 8080
```
### Launch keyboard server in mirror mode.
Keyboard based input session can be executed on both host as well as remote client. This doesn't block keyboard input to host. By default mirror mode is disabled.
```javascript
KeyboardServer -m true
```
## Keyboard Client
Keyboard Client will perform all keystrokes from keyboard server.
### Default configuration on default port (9022)
```javascript
KeyboardClient -c 192.168.1.2
```
## Keyboard Client with different port
```javascript
KeyboardClient -c 192.168.1.2 -p 8080
``` 
