"""

Comandos soportados:
  ISDARK   -> lee la fotoresistencia (LDR) y responde DARK o LIGHT
  READID   -> espera hasta 10s una tarjeta/llavero RFID y responde el UID (o TIMEOUT)
  LEDON    -> enciende el LED verde, responde OK
  LEDOFF   -> apaga el LED verde, responde OK

Requiere el archivo mfrc522.py (fork de danjperron/micropython-mfrc522,
el que SI soporta la plataforma 'rp2') subido junto a este archivo en la Pico.
Fuente: https://github.com/danjperron/micropython-mfrc522/blob/master/mfrc522.py
"""

import sys
from machine import Pin, ADC
import time
from mfrc522 import MFRC522

# Configuracion de pines (segun el pinout):
ldr = ADC(Pin(27))          # GP27 - nodo entre 3.3V-LDR-10k-GND
led = Pin(21, Pin.OUT)      # GP21 - LED verde (via resistencia)


rfid = MFRC522(sck=6, mosi=7, miso=4, rst=22, cs=5, spi_id=0)

UMBRAL_OSCURIDAD = 20000    # ajustar segun pruebas reales con la LDR
TIMEOUT_READID_MS = 10000   # 10 segundos para acercar la tarjeta


def manejar_isdark():
    valor = ldr.read_u16()  # rango 0-65535 en RP2040
    oscuro = valor < UMBRAL_OSCURIDAD
    print("DARK" if oscuro else "LIGHT")


def manejar_readid():
    inicio = time.ticks_ms()
    while time.ticks_diff(time.ticks_ms(), inicio) < TIMEOUT_READID_MS:
        rfid.init()
        (estado, tipo_tarjeta) = rfid.request(rfid.REQIDL)
        if estado == rfid.OK:
            (estado, uid) = rfid.SelectTagSN()
            if estado == rfid.OK:
                uid_str = "".join("{:02X}".format(b) for b in uid)
                print(uid_str)
                return
        time.sleep_ms(100)
    print("TIMEOUT")


def manejar_ledon():
    led.on()
    print("OK")


def manejar_ledoff():
    led.off()
    print("OK")


def main():
    while True:
        comando = sys.stdin.readline().strip()

        if comando == "ISDARK":
            manejar_isdark()
        elif comando == "READID":
            manejar_readid()
        elif comando == "LEDON":
            manejar_ledon()
        elif comando == "LEDOFF":
            manejar_ledoff()
        elif comando:
            print("ERROR_COMANDO_DESCONOCIDO")


main()
