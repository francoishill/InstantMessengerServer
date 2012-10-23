:: If you installed OpenSSL in non-default directory, you MUST change paths in commands.

@echo off
set OPENSSL_CONF=C:\Users\francois\Dropbox\Other\Handy downloads\Applications\Windows\OpenSSL-Win64\bin\openssl.cfg

"C:\Users\francois\Dropbox\Other\Handy downloads\Applications\Windows\OpenSSL-Win64\bin\openssl.exe" req -x509 -nodes -days 365 -newkey rsa:1024 -keyout private.key -out cert.crt

"C:\Users\francois\Dropbox\Other\Handy downloads\Applications\Windows\OpenSSL-Win64\bin\openssl.exe" pkcs12 -export -in cert.crt -inkey private.key -out server.pfx -passout pass:instant

del .rnd
del private.key
del cert.crt