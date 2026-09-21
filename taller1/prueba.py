#para correr en el rasp: 
from mfrc522 import MFRC522
rfid = MFRC522(sck=6, mosi=7, miso=4, rst=22, cs=5, spi_id=0)
version = rfid._rreg(0x37)
print(hex(version))

import time
for _ in range(30):
  (estado, tipo) = rfid.request(rfid.REQIDL)
  if estado == rfid.OK:
    (estado2, uid) = rfid.SelectTagSN()
    if estado2 == rfid.OK:
      uid_str = "".join("{:02X}".format(b) for b in uid)
      print("UID:", uid_str)
      break
  time.sleep(0.3)
