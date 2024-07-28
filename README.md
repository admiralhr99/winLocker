# Windows Locker

## Description

The **WinLocker** is a C# application designed for situations where you do not want lock your pc while you step away. For example, if you are in an office and need to get coffee but don't want to lock your PC due to the inconvenience of typing a complex password, this application provides a solution.

When running this application:
- The mouse input will be disabled.
- The application will remain on top of all other windows.
- Shortcut keys and Task Manager will be disabled.
- The application will revert these settings to default once you type your password and submit it.

**Note**: This application requires administrator privileges to function correctly.

**Note2**: The password is hardcoded. You can change it in the source code.


## Installation

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/admiralhr99/winLocker.git
   cd winLocker
   dotnet build
   dotnet run
    ```
   
