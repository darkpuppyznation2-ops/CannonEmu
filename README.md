# Cannon Emu

DISCLAIMER: CannonEmu is not related to Retroarch in anyway except for the emulation frontend idea!

## Features

- **Serial Communication**: Connect to emulator via COM port with configurable baud rates
- **Command Interface**: Send control commands (Reset, Start, Stop, Fire)
- **Live Display**: Real-time logging of transmitted and received data
- **Dark Theme UI**: Professional dark-themed interface
- **Auto COM Port Detection**: Automatically detects available COM ports

## Requirements

- .NET 8.0 or later
- Windows operating system
- Serial port device (emulator)

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

## Usage

1. **Select COM Port**: Choose the appropriate COM port from the dropdown
2. **Configure Baud Rate**: Select baud rate (default: 9600)
3. **Connect**: Click "Connect" to establish serial connection
4. **Send Commands**: Use Control buttons to send commands to the emulator
5. **Monitor Output**: View real-time communication in the Display panel

## Architecture

- **MainWindow.cs**: Primary UI and serial communication handler
- **Async Serial Reading**: Non-blocking data reception with cancellation support
- **Command Queue**: Extensible command system for emulator control

## Future Enhancements

- Command scripting engine
- Data logging to file
- Graphical emulator state visualization
- Telemetry dashboard
- Configuration profiles
