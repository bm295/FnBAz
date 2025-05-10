@echo off
cd /d %~dp0

REM Use specific Python 3.10 path
REM set PYTHON=C:\Users\Minh\AppData\Local\Programs\Python\Python310\python.exe

IF NOT EXIST .venv (
    echo Creating virtual environment using Python 3.10...
    python -m venv .venv
)

call .venv\Scripts\activate

REM echo Installing dependencies...
REM pip install -r requirements.txt

python main.py --input input.txt --train

pause
