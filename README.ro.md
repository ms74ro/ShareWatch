# ShareWatch

[English](README.md)

ShareWatch este o aplicație mică pentru Windows, în tray, care te anunță când un alt calculator din rețea îți deschide folderele partajate. Fiecare notificare arată adresa IP și numele calculatorului, iar fiecare accesare este scrisă într-un jurnal.

Este un singur executabil, fără instalare și fără alte dependențe în afară de ce vine cu Windows (.NET Framework 4).

![Tabul Monitorizare](docs/screenshots/en/monitor.png)

Alte capturi (în engleză și română, temă luminoasă, date fictive) sunt în [docs/screenshots](docs/screenshots).

## Funcții

- **Notificări în timp real** când un calculator se conectează la un folder partajat, cu IP și nume, sunet discret opțional și durată configurabilă.
- **Tab Monitorizare**: calculatoarele conectate acum. Clic dreapta pe unul pentru a-l deconecta sau bloca.
- **Foldere monitorizate**: alegi ce partajări sunt urmărite. Poți partaja un folder nou sau opri o partajare direct din aplicație.
- **Ascunde partajările administrative** (C$, ADMIN$ etc.) din liste și din meniul tray. Activat implicit.
- **Listă de blocare**: calculatoarele din listă sunt deconectate imediat ce se conectează. Salvată în `blocklist.txt`, editabilă din aplicație sau direct în fișier.
- **Blocare opțională în Windows Firewall**: o regulă de intrare (`ShareWatch - blocare SMB`) blochează TCP 139/445 de la IP-urile blocate.
- **Istoric**: jurnalul accesărilor în `acces_foldere.log`.
- **Pornire automată cu Windows** direct în tray, cu drepturi de administrator și fără UAC.
- Opțional: deschiderea ferestrei cu un singur clic pe iconița din tray.
- Temă luminoasă / întunecată / automată; română și engleză integrate, alte limbi din fișiere `language\xx.json` (vezi [Limbi](#limbi)).

## Cerințe

- Windows 10 sau 11 (.NET Framework 4.x, inclus în Windows).
- Drepturi de administrator. Fără ele Windows nu permite citirea sesiunilor SMB, deci monitorizarea nu funcționează.

## Utilizare

1. Descarcă `ShareWatch.exe` și pune-l într-un folder la alegere.
2. Rulează-l și acceptă cererea de administrator.
3. Iconița apare în tray. Dublu-clic pe ea deschide fereastra.

Setările se salvează în `sharewatch.ini` lângă executabil (sau în `%LOCALAPPDATA%\ShareWatch` dacă folderul nu permite scrierea).

## Limbi

Româna și engleza sunt integrate în executabil. Alte limbi se încarcă din fișiere JSON aflate într-un folder `language` lângă `ShareWatch.exe`:

```
ShareWatch.exe
language\
    de.json
    fr.json
```

Pentru a adăuga o limbă:

1. Copiază [`language/en.json`](language/en.json) cu numele `xx.json`, unde `xx` este codul limbii din două litere (`de.json`, `fr.json` etc.).
2. Completează `_name` cu numele limbii, așa cum va apărea în Opțiuni (de ex. `Deutsch`).
3. Tradu doar valorile. Cheile (textul în română) și marcajele de tipul `{0}` rămân neschimbate.
4. Repornește ShareWatch și alege limba din Opțiuni → Limbă. La **Automat**, se folosește limba din Windows, dacă există fișier pentru ea.

Textele care lipsesc dintr-un fișier apar în engleză, iar un fișier JSON invalid este ignorat. `ro.json` și `en.json` nu se încarcă niciodată (ambele limbi sunt integrate), deci `en.json` poate rămâne în folder ca model.

## Alerte antivirus

Unele programe antivirus pot semnala `ShareWatch.exe` ca suspect. O versiune mai veche a fost semnalată de cel puțin un antivirus. Este o alertă falsă: scanerele euristice reacționează la ce trebuie să facă aplicația, plus faptul că executabilul e mic, nou și nesemnat digital:

- cere drepturi de administrator și pornește cu Windows printr-o sarcină programată;
- rulează ascuns, în tray;
- citește sesiunile SMB și poate deconecta calculatoare;
- poate adăuga o regulă în Windows Firewall.

Pe VirusTotal (versiunea 0.6.2, noiembrie 2026), 5 motoare o semnalează. Toate cele 5 rezultate vin din analiză euristică sau de tip machine learning, nu din semnătura unui virus cunoscut:

| Motor | Rezultat |
|---|---|
| Bkav Pro | W32.Malware.D226EE45 |
| Elastic | Malicious (high Confidence) |
| SecureAge | Malicious |
| Symantec | ML.Attribute.HighConfidence |
| Trapmine | Suspicious.low.ml.score |

Celelalte motoare, de exemplu Acronis (Static ML), o raportează ca fiind curată. Nici **Bitdefender Antivirus Plus**, pe PC-ul autorului, nu o semnalează. Dacă antivirusul tău o face:

1. Citește codul sursă. Este tot în acest depozit, fără biblioteci externe.
2. Compilează singur executabilul cu `.\build.ps1` (vezi mai jos), în loc să-l descarci.
3. Sau verifică dacă fișierul descărcat este cel din release. Amprenta SHA-256 e trecută în notele release-ului. Compar-o cu:

   ```powershell
   Get-FileHash .\ShareWatch.exe -Algorithm SHA256
   ```

4. Poți scana fișierul pe [VirusTotal](https://www.virustotal.com) și poți raporta alerta falsă producătorului antivirusului.

## Compilare

Nu e nevoie de Visual Studio. Scriptul folosește `csc.exe` din .NET Framework 4, inclus în Windows:

```powershell
.\build.ps1
```

`.\build.ps1 -Test` creează `ShareWatch-test.exe` fără manifestul de administrator, doar pentru testarea interfeței.

Capturile din README provin din varianta de test, cu partajări, calculatoare și jurnal fictive (codul e în `Demo.cs`, compilat doar în varianta de test). Pune exe-ul de test într-un folder cu un `sharewatch.ini` care conține `theme=1` și `lang=1` (sau `lang=2` pentru engleză), apoi rulează:

```powershell
.\ShareWatch-test.exe --screenshots .\capturi
```

Salvează câte un PNG pentru fiecare tab și pentru notificare, apoi se închide.

`language/en.json` se generează din textele în engleză integrate. După ce adaugi sau modifici texte, regenerează-l cu varianta de test:

```powershell
.\ShareWatch-test.exe --export-lang language\en.json
```

## Fișiere

| Fișier | Conținut |
|---|---|
| `ShareWatch.cs` | Punct de intrare, setări, NetApi (partajări și sesiuni) |
| `MainForm.cs` | Fereastra principală și taburile |
| `Tray.cs` | Iconița din tray, meniul, bucla de monitorizare |
| `Toast.cs` | Fereastra de notificare |
| `ShareDialog.cs` | Partajare / oprire partajare folder |
| `BlockList.cs` | Lista de blocare |
| `Firewall.cs` | Regula din Windows Firewall |
| `Support.cs` | Traduceri și funcții ajutătoare |
| `Demo.cs` | Date fictive și capturi (doar în varianta de test) |
| `build.ps1` | Scriptul de compilare |

## Modificări

Vezi [CHANGELOG.md](CHANGELOG.md) (în engleză).

## Contributori

- [ms74ro](https://github.com/ms74ro) — autor
- [Claude](https://claude.com/claude-code) (Anthropic) — cod și documentație, împreună cu ms74ro

## Licență

[MIT](LICENSE) © 2026 ms74ro ([traducerea în română](LICENTA.md), doar informativă). Poți folosi, modifica și redistribui aplicația liber, inclusiv comercial, cu condiția să păstrezi mențiunea de copyright și textul licenței. Aplicația este oferită fără nicio garanție.
