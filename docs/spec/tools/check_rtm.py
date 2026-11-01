"""Sprawdzenie Macierzy Pokrycia Wymagań (sekcja 18 docs/ARCHITECTURE_DETAILED.md). Uruchomienie: python3 docs/spec/tools/check_rtm.py"""
import re,os,sys,glob
R=os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','..','..')+'/'
doc=open(R+'docs/ARCHITECTURE_DETAILED.md').read()
api=open(R+'docs/spec/api.md').read(); ddl=open(R+'docs/spec/ddl.md').read()
req=open(R+'docs/requirements.md').read(); gl=open(R+'docs/glossary.md').read()
spec_all=doc+api+ddl+open(R+'docs/spec/ef-core.md').read()+open(R+'docs/spec/prompts.md').read()+''.join(open(f).read() for f in glob.glob(R+'docs/adr/*.md'))
bledy=[]
sec=doc[doc.index('## 18. Macierz Pokrycia'):doc.index('### 18.5')]
def tabela(naglowek):
    t=sec[sec.index(naglowek):]; t=t[t.index('\n|'):]
    rows=[]
    for l in t.split('\n')[1:]:
        if not l.startswith('|'):
            if rows: break
            continue
        if l.startswith('|---') or l.startswith('| ID') or l.startswith('| Pojęcie'): continue
        rows.append([c.strip() for c in l.strip('|').split(' | ')])
    return rows
fr=tabela('### 18.1'); nfr=tabela('### 18.2'); odw=tabela('### 18.4')
wym=re.findall(r'\*\*((?:N?FR)-\d\d)\*\*',req)
ids=[r[0] for r in fr+nfr]
for w in wym:
    if ids.count(w)!=1: bledy.append(f'wymaganie {w}: {ids.count(w)} wierszy')
for x in ids:
    if x not in wym: bledy.append(f'wiersz {x} nie istnieje w requirements.md')
# katalog 12.3
kat=doc[doc.index('### 12.3 Katalog endpointów'):doc.index('### 12.4')]
endp=set()
for l in kat.split('\n'):
    m=re.match(r'\| (GET|POST|PUT|PATCH|DELETE) \| (.*?) \|',l)
    if m:
        for p in re.findall(r'`([^`]+)`',m.group(2)): endp.add(f'{m.group(1)} {p}')
tabele=set(re.findall(r'CREATE TABLE ([a-z_]+)',ddl))
str7=api[api.index('## 7. Mapowanie stron'):]
strony=set()
for l in str7.split('\n'):
    if l.startswith('| `'):
        pierwsza=l.split(' | ')[0]
        strony|=set(re.findall(r'`([^`]+)`',pierwsza))
bramki=set(re.findall(r'^\| (G-\d) \|',doc,re.M))
licz={'endpointy':0,'tabele':0,'strony':0,'adr':0,'bramki':0,'identyfikatory':0}
for r in fr+nfr:
    if len(r)!=9: bledy.append(f'{r[0]}: {len(r)} kolumn'); continue
    for e in re.findall(r'`([^`]+)`',r[5]):
        licz['endpointy']+=1
        if e not in endp: bledy.append(f'{r[0]}: endpoint {e} nie ma w 12.3')
    for t in re.findall(r'`([^`]+)`',r[4]):
        licz['tabele']+=1
        if t not in tabele: bledy.append(f'{r[0]}: tabela {t} nie ma w ddl.md')
    for st in re.findall(r'`([^`]+)`',r[6]):
        licz['strony']+=1
        if st not in strony and not (st.startswith('admin/') and st[6:] in strony): bledy.append(f'{r[0]}: strona {st} nie ma w api.md 7')
    for a in re.findall(r'ADR-\d{3}',r[7]+r[8]):
        licz['adr']+=1
        if not glob.glob(R+f'docs/adr/{a}-*.md'): bledy.append(f'{r[0]}: brak pliku {a}')
    for g in re.findall(r'G-\d',r[8]):
        licz['bramki']+=1
        if g not in bramki: bledy.append(f'{r[0]}: bramka {g}')
    for ident in re.findall(r'`([A-Za-z][A-Za-z0-9_.\-]+)`',r[2]+r[3]):
        licz['identyfikatory']+=1
        if spec_all.count(ident)<2: bledy.append(f'{r[0]}: identyfikator {ident} nie występuje poza RTM')
    for t in re.findall(r'`([^`]+\.spec\.ts|[A-Z][A-Za-z0-9]+)`',r[8]):
        pass
# macierz odwrotna
pojecia=re.findall(r'^\| \*\*(.+?)\*\* \|',gl,re.M)
odw_poj=[r[0] for r in odw]
for p in pojecia:
    if p not in odw_poj: bledy.append(f'pojęcie {p} bez wiersza macierzy odwrotnej')
def rozwin(txt):
    out=set()
    for a,b in re.findall(r'((?:N?FR)-\d\d)(?: … ((?:N?FR)-\d\d))?',txt):
        if b:
            pre=a[:a.index('-')+1]; out|={f'{pre}{n:02d}' for n in range(int(a[-2:]),int(b[-2:])+1)}
        else: out.add(a)
    return out
pokryte=set()
for r in odw:
    for ident in re.findall(r'`([A-Za-z_][A-Za-z0-9_.]*)`',r[1]):
        for cz in ident.split('.'):
            if spec_all.count(cz)<2: bledy.append(f'{r[0]}: identyfikator {cz}')
    w=rozwin(r[2]); pokryte|=w
    for x in w:
        if x not in wym: bledy.append(f'{r[0]}: wymaganie {x} nie istnieje')
# (9) ADR-075: strony i testy akceptacyjne mają angielskie nazwy (brak segmentów z wersji 3.3)
STARE={'publiczne','uczen','nauczyciel','logowanie','rejestracja','reset-hasla','potwierdzenie','dolacz','test-diagnostyczny','sciezka-nauki','wkladki-teoretyczne','zadania','rozwiazania','profil','przypisania','klasy','ustawienia','uczniowie','zestawy-zadan','weryfikacje','nowe','generowanie','przeglad','zgloszenia-bledu-zadan','uzytkownicy','arkusze-cke','importy','konfiguracje-modeli','progi-pewnosci','kalibracja','bezpieczniki','obciazenie-weryfikacji','pula-diagnostyczna','zlecenia-generowania','praca-w-tle'}
for r in fr+nfr:
    for st in re.findall(r'`([^`]+)`',r[6]):
        for seg in re.split(r'[/()\[\]]',st):
            if seg in STARE or seg=='kod': bledy.append(f'{r[0]}: strona {st} ma segment z wersji 3.3: {seg}')
            if re.search(r'^(klasa|zadanie|rozwiazanie|wkladka|weryfikacja|uczen|zestaw|zlecenie|przypisanie)Id$',seg): bledy.append(f'{r[0]}: strona {st}: polski parametr {seg}')
    for sp in re.findall(r'`([a-z0-9\-]+)\.spec\.ts`',r[8]):
        if any(tok in STARE or tok in {'konto','poziom','termin','rekomendacja','wkladka','trudnosc','powtorki','wybor','dzialu','uzasadnienie','tresc','kryteria','walidacja','zamkniete','przeslanie','ocena','krokowa','wynik','rozumowania','wynikanie','krokow','pewnosc','wstepna','bledne','kroki','dowod','wskazowka','blad','odczytu','po','ocenie','klasa','postepy','weryfikacja','zdjecia','korekta','otagowanie','sesja','dwie','karty','za','aparat','przeladowanie','siec','komorkowa','autozapis','wolna','podwojne','klikniecie','zapasowe','odpytywanie','kontrakt','wygasla','powiadomienia'} for tok in sp.split('-')):
            bledy.append(f'{r[0]}: test {sp}.spec.ts ma polską nazwę pliku')
# (10) odnośniki Markdown w docs/ wskazują istniejące pliki
for md in glob.glob(R+'docs/**/*.md',recursive=True):
    txt=open(md).read()
    for cel in re.findall(r'\]\(([^)#\s]+)(?:#[^)]*)?\)',txt):
        if re.match(r'^[a-z]+:',cel): continue
        p=os.path.normpath(os.path.join(os.path.dirname(md),cel))
        if not os.path.exists(p): bledy.append(f'{os.path.relpath(md,R)}: martwy odnośnik {cel}')
print('wymagania w requirements.md:',len(wym),'| wiersze RTM:',len(ids),'| pojęcia słownika:',len(pojecia),'| wiersze odwrotne:',len(odw))
print('sprawdzone odwołania:',licz)
print('wymagania pokryte przez macierz odwrotną:',len(pokryte),'z',len(wym),'; bez pojęcia:',sorted(set(wym)-pokryte))
print('BŁĘDY:',len(bledy)); [print(' -',b) for b in bledy]
sys.exit(1 if bledy else 0)
