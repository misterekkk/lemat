"""Sprawdzenie Macierzy Pokrycia Wymagań (sekcja 18 docs/ARCHITECTURE_DETAILED.md), wersja 3.5: sprawdzenia (1)–(17). Uruchomienie: python3 docs/spec/tools/check_rtm.py"""
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
# ---------------- wersja 3.5 (C9-05, C9-06): sprawdzenia (11)–(17) ----------------
def rozwin2(txt):
    out=set()
    for a,b in re.findall(r'((?:N?FR)-\d\d)(?: ?… ?((?:N?FR)-\d\d))?',txt):
        if b:
            pre=a[:a.index('-')+1]; out|={f'{pre}{n:02d}' for n in range(int(a[-2:]),int(b[-2:])+1)}
        else: out.add(a)
    return out
# drzewo 6.8 → strony (pełne ścieżki względem src/app, z grupą)
drzewo=doc[doc.index('### 6.8 Struktura'):doc.index('### 6.9')]
strony_drzewa=set(); stos={}
for l in drzewo.split('\n'):
    m=re.match(r'^([│ ]*)[├└]── (.*)$',l)
    if not m: continue
    gl=len(m.group(1))//4
    for tok in m.group(2).split('#')[0].split():
        sciezka='/'.join(stos[d] for d in range(gl) if d in stos)
        if tok.endswith('/'):
            stos[gl]=tok.rstrip('/'); [stos.pop(k) for k in list(stos) if k>gl]
        if tok.endswith('page.tsx'): strony_drzewa.add(re.sub(r'^.*?src/app/?','',(sciezka+'/'+tok[:-8]).rstrip('/')))
mapa={}; kazda=''
for l in str7.split('\n'):
    if l.startswith('| `') or l.startswith('| każda'):
        c=[x.strip() for x in l.strip().strip('|').split(' | ')]
        if l.startswith('| każda'): kazda=c[2]+' '+c[3]; continue
        for st in re.findall(r'`([^`]+)`',c[0]):
            if st.startswith('?'): continue
            mapa[st]=mapa.get(st,'')+' '+c[2]+' '+c[3]
# (13) notacja stron: pełna ścieżka z grupą, zgodna z drzewem 6.8; każda strona drzewa ma wiersz w api.md 7
for st in mapa:
    if st not in strony_drzewa: bledy.append(f'api.md 7: strona {st} nie jest pełną ścieżką pliku page.tsx z drzewa 6.8')
for st in sorted(strony_drzewa-set(mapa)): bledy.append(f'api.md 7: brak wiersza dla strony {st}')
kat_wym={}
for l in kat.split('\n'):
    m=re.match(r'\| (GET|POST|PUT|PATCH|DELETE) \| (.*?) \| (.*?) \| (.*?) \| (.*?) \| (.*?) \|',l)
    if m:
        for p_ in re.findall(r'`([^`]+)`',m.group(2)): kat_wym[f'{m.group(1)} {p_}']=rozwin2(m.group(6))
def uzywa(st,e):
    meth,path=e.split(' ',1); txt=mapa.get(st,'')+' '+kazda
    for t in (txt, txt.replace('`','')):
        if re.search(re.escape(f'{meth} {path}')+r'(?![\w/{-])',t): return True
    segs=path.split('/')
    for k in range(1,len(segs)):
        if re.search(r'(?:'+meth+r' |, |^|`)…/'+re.escape('/'.join(segs[-k:]))+r'(?![\w/-])',txt): return True
    return False
wiersze={r[0]:r for r in fr+nfr if len(r)==9}
for e,w in kat_wym.items():
    if not w: bledy.append(f'12.3: {e} bez wymagania w kolumnie „Wymagania”')
    for x in w:   # (11b) 12.3 → RTM
        if x in wiersze and f'`{e}`' not in wiersze[x][5]: bledy.append(f'{x}: endpoint {e} ma {x} w 12.3, a nie ma go w wierszu RTM')
for rid,r in wiersze.items():
    eps=re.findall(r'`((?:GET|POST|PUT|PATCH|DELETE) [^`]+)`',r[5]); sts=re.findall(r'`([^`]+)`',r[6])
    for e in eps:
        if e in kat_wym and rid not in kat_wym[e]: bledy.append(f'{rid}: endpoint {e} bez {rid} w kolumnie „Wymagania” 12.3')   # (11a)
        if e in kat_wym and sts and not any(uzywa(st,e) for st in sts): bledy.append(f'{rid}: endpoint {e} nie jest używany przez żadną stronę wiersza (api.md 7)')   # (14)
    for st in sts:
        if st not in strony_drzewa: bledy.append(f'{rid}: strona {st} nie jest pełną ścieżką z grupą z drzewa 6.8')   # (13)
    # (15) moduł: projekty/przestrzenie i typy prac
    poza=spec_all.replace(sec,'')
    for pr in re.findall(r'Lemat\.[A-Za-z.]+[A-Za-z]',r[2]):
        if pr not in poza: bledy.append(f'{rid}: projekt/przestrzeń {pr} nie występuje poza RTM')
    prace=set(re.findall(r'^\| `([A-Z][A-Z0-9_]+)` \|',doc[doc.index('### 13.3'):doc.index('### 13.4')],re.M))
    for pr in re.findall(r'\(`([A-Z][A-Z0-9_]+)`\)',r[2]):
        if pr not in prace: bledy.append(f'{rid}: typ pracy {pr} poza 13.3')
    # (16) Agent / plugin / prompt
    prompty=open(R+'docs/spec/prompts.md').read()
    for ag in re.findall(r'`(I[A-Z][A-Za-z]+|[A-Z][A-Za-z]+Plugin|Circuit[A-Za-z]+)`',r[3]):
        if ag not in poza: bledy.append(f'{rid}: agent/port/plugin {ag} nie występuje poza RTM')
    for pid in re.findall(r'`([a-z\-]+-v\d)`',r[3]):
        if pid not in prompty: bledy.append(f'{rid}: prompt {pid} nie istnieje w prompts.md')
    # (17) Weryfikacja: testy Playwright w 6.11.4, testy C# poza RTM
    t6114=doc[doc.index('#### 6.11.4'):doc.index('#### 6.11.5')]
    for sp in re.findall(r'`([a-z0-9\-]+)\.spec\.ts`',r[8]):
        if f'`{sp}`' not in t6114 and f'{sp}.spec.ts' not in doc.replace(sec,''): bledy.append(f'{rid}: test {sp}.spec.ts bez wpisu w 6.11.4')
    for t in re.findall(r'`([A-Z][A-Za-z0-9]+)`',r[8]):
        if t not in poza: bledy.append(f'{rid}: test {t} zdefiniowany wyłącznie w RTM')
# (12) adresy stron w kodzie listingów (ts/tsx) = adresy stron drzewa 6.8 (C9-05)
def wzorzec(st):
    seg=[x for x in st.split('/') if x and not (x.startswith('(') and x.endswith(')'))]
    return '^/'+'/'.join('[^/]+' if x.startswith('[') else re.escape(x) for x in seg)+'$'
wzorce=[wzorzec(st) for st in strony_drzewa]
adresy=0
for md in [R+'docs/ARCHITECTURE_DETAILED.md',R+'docs/spec/api.md']+glob.glob(R+'docs/adr/ADR-0[4-9]*.md'):
    for blok in re.findall(r'```(?:ts|tsx)\n(.*?)```',open(md).read(),re.S):
        for u in re.findall(r'(?:assign|push|replace|redirect|toHaveURL)\(\s*["\'`](/[^"\'`?#$]*)',blok)+re.findall(r'href=\{?["\'`](/[^"\'`?#$]*)',blok):
            adresy+=1
            if u.startswith('/api/'): continue
            if not any(re.match(w,u.rstrip('/') or '/') for w in wzorce) and u!='/':
                bledy.append(f'{os.path.relpath(md,R)}: adres strony {u} w kodzie nie istnieje w drzewie 6.8')
licz['adresy_stron_w_kodzie']=adresy
licz['strony_drzewa']=len(strony_drzewa)
print('wymagania w requirements.md:',len(wym),'| wiersze RTM:',len(ids),'| pojęcia słownika:',len(pojecia),'| wiersze odwrotne:',len(odw))
print('sprawdzone odwołania:',licz)
print('wymagania pokryte przez macierz odwrotną:',len(pokryte),'z',len(wym),'; bez pojęcia:',sorted(set(wym)-pokryte))
print('BŁĘDY:',len(bledy)); [print(' -',b) for b in bledy]
sys.exit(1 if bledy else 0)
