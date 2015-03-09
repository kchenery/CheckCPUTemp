# CheckCPUTemp
Nagios CPU temperature check for windows

## Parameters
| Parameter | Description |
|-----------|-------------|
| c | Critical temperature value |
| w | Warning temperature value |

## Usage
Run CheckCPUTemp with a warning at 60°C and 70°C
```
CheckCPUTemp.exe -w 60 -c 70
```

### Example Output
```
CPU Temp OK - Temperature = 50.2 | CPUTemp=50.2;60.0;70.0;
```
