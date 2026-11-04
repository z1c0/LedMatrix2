import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, random, png
from draw import *
OUT=sys.argv[1]; icons={}
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco
def paint(m,cols):
    return [(m[k],v) for k,v in cols.items() if k in m]
def apply(c,m,cols):
    for k,v in cols.items():
        if k in m: c.fill(m[k],v)
def star(cx,cy,ro,ri,rot=-90,n=5):
    return poly([(cx+(ro if i%2==0 else ri)*math.cos(math.radians(rot+i*180/n)),cy+(ro if i%2==0 else ri)*math.sin(math.radians(rot+i*180/n))) for i in range(2*n)])
def vgrad(c,top,bot):
    t=hx(top); b=hx(bot)
    for y in range(32):
        k=y/31; c.p[y]=[tuple(round(t[i]+(b[i]-t[i])*k) for i in range(3))+(255,) for _ in range(32)]
FONT={'I':"###|.#.|.#.|.#.|###",'H':"#.#|#.#|###|#.#|#.#",'A':".#.|#.#|###|#.#|#.#",'V':"#.#|#.#|#.#|#.#|.#.",
'E':"###|#..|##.|#..|###",'T':"###|.#.|.#.|.#.|.#.",'P':"##.|#.#|##.|#..|#..",'O':".#.|#.#|#.#|#.#|.#.",
'W':"#...#|#...#|#.#.#|#.#.#|.#.#.",'R':"##.|#.#|##.|#.#|#.#",'!':"#|#|#|.|#",' ':"..|..|..|..|.."}
def txt(s,y):
    w=sum(len(FONT[ch].split('|')[0])+1 for ch in s)-1; x=(32-w)//2; m=set()
    for ch in s:
        rows=FONT[ch].split('|')
        for r,row in enumerate(rows):
            for k,v in enumerate(row):
                if v=='#': m.add((x+k,y+r))
        x+=len(rows[0])+1
    return m

# ───────────── Marvel, part 2 ─────────────
@icon('marvel_black_widow_logo')
def _(c):
    c.fill(disc(16,16,15.6),'5a0a0a'); c.fill(disc(16,16,14.2),'1c1c20')
    hg=poly([(7,5),(25,5),(16,15.2)])|poly([(16,16.8),(25,27),(7,27)])
    c.fill(hg,'e01818'); c.fill(edge(hg)&{(x,y) for x,y in hg if x>16},'a00c0c')

@icon('marvel_captain_marvel')
def _(c):
    c.fill(rect(0,0,31,31),'101c4a'); c.fill(rect(0,0,31,10),'b01020')
    rays=poly([(0,13),(10,14.5),(10,17.5),(0,19)])|poly([(32,13),(22,14.5),(22,17.5),(32,19)])
    rays|=poly([(4,1),(13,11),(11,13)])|poly([(28,1),(19,11),(21,13)])
    c.fill(rays,'f0b020')
    s=star(16,16.5,9,3.8); c.fill(outline(s),'101c4a'); c.fill(s,'ffd040'); c.fill(edge(s)&{(x,y) for x,y in s if y>17},'d89010')

@icon('marvel_eye_of_agamotto')
def _(c):
    for a in range(0,360,45):
        r=math.radians(a); c.fill(poly([(16+11*math.cos(r-0.25),17+11*math.sin(r-0.25)),(16+15.5*math.cos(r),17+15.5*math.sin(r)),(16+11*math.cos(r+0.25),17+11*math.sin(r+0.25))]),'b88418')
    c.fill(disc(16,17,12.2),'e0a828'); c.fill(edge(disc(16,17,12.2)),'8a5a10')
    c.fill(disc(16,17,9.6),'2a1a08')
    eye=ell(16,17,8.2,4.4); c.fill(eye,'f0e0a0'); c.fill(edge(eye),'e0a828')
    c.fill(disc(16,17,3.4),'30e060'); c.fill(disc(16,17,1.4),'0a3a14'); c.put(14,15,'d0ffd0')
    c.fill(outline(disc(16,17,3.4))&ell(16,17,7.6,3.9),'1a8a3a')
    c.fill(rect(15,0,16,4),'b88418')

@icon('marvel_wolverine_mask')
def _(c):
    m=art(["................",
           ".K..............",
           ".KK.............",
           ".KKK............",
           "..KKK...........",
           "..KKKK..........",
           "..KKKKK...YYYYYY",
           "...KKKKKYYYYYYYY",
           "...KKKKKKYYYYYYY",
           "...YKKKKKKYYYYYY",
           "...YYKKKKKKKYYYY",
           "...YYYKKWWWKKYYY",
           "...YYYYKWWWWKYYY",
           "...YYYYYKKKKYYYY",
           "...YYYYYYYYYYYYY",
           "...HYYYYYYYYYYYY",
           "...HHSSSSSSSYYYY",
           "...HHSSSSSSSSSYY",
           "...HHHSSSSSSSSSS",
           "...HHHSSSSSSSSSS",
           "....HHSSSSSSSSSS",
           "....HHSSSSSSSSSS",
           ".....HSSSSSSSDDD",
           ".....HSSSSSSSSSS",
           "......SSSSSSSSSS",
           ".......SSSSSSSSS",
           "........SSSSSSSS",
           "..........SSSSSS"],0,2,mir=True)
    apply(c,m,dict(K='2a3448',Y='f0c020',W='f8f8f8',H='4a2a14',S='e0a878',D='8a4a30'))
    c.fill(edge(m['K'])-dilate(m['Y']),'5a6a8a')
    c.fill(outline(m['Y'])&m['S'],'c08a5a')

@icon('marvel_black_panther')
def _(c):
    head=ell(16,17.5,11,13.2)|mirror(poly([(5.5,9),(7,2),(11.5,6)]))
    c.fill(head,'22222e'); c.fill(edge(head),'4a4a60')
    L=mirror(polyline([(16,4),(12,8),(9.5,12.5)])|polyline([(12.5,20),(16,23)])|polyline([(7,20),(10,27),(16,30)]))
    L|=line(16,4,16,13)
    c.fill(L&head,'b070ff')
    c.fill(mirror(poly([(7.5,13),(13.5,14.5),(13,16.5),(8.5,15.5)])),'f4f4f4')
    for x in range(8,25,2): c.put(x,31,'d0d0e0')

@icon('marvel_baby_groot')
def _(c):
    pot=poly([(8,22),(24,22),(22,31.5),(10,31.5)]); c.fill(pot,'c0602a'); c.fill(rect(7,22,24,24),'d8743a')
    c.fill({(x,y) for x,y in pot if x>=20},'8a4018'); c.fill(rect(9,23,22,23),'3a2010')
    c.fill(rect(14,17,17,22),'7a4a22'); c.fill(line(14,19,9,15),'7a4a22'); c.fill(line(18,19,23,14),'7a4a22')
    h=ell(16,12,7.5,8); c.fill(h,'8a5a2a')
    for x in (11,14,18,21): c.fill({(x,y) for y in range(5,20)}&h,'6a4018')
    c.fill(edge(h),'4a2a10')
    for x in (12,19): c.fill(rect(x,11,x+1,13),'101010'); c.put(x,11,'f0f0f0')
    c.fill({(15,16),(16,17),(17,17),(18,16)},'3a2010')
    c.fill(poly([(11,5),(7,1),(12,2)]),'40c040'); c.fill(poly([(20,4),(25,0),(23,4.5)]),'60e050'); c.fill(line(16,4,16,1),'6a4018'); c.fill(poly([(16,1.5),(19,0),(18.5,2.5)]),'40c040')

@icon('marvel_hulk')
def _(c):
    m=art(["......KKKKKKKKKK",
           "....KKKKKKKKKKKK",
           "...KKKKKKKKKKKKK",
           "..KKKKKKKKKKKKKK",
           "..KKKKKKKKKKKKKK",
           "..KKKKGKKKKKKGKK",
           "..KKKGGGKKGKGGGG",
           "..KKGGGGGGGGGGGG",
           "..KGGGGGGGGGGGGG",
           "..KGGBBBGGGGGGGG",
           "..KGGGGBBBBGGGGG",
           "...GGGGGGBBBBDGG",
           "...GGWWKKGGGBDGG",
           "...GGGGGGGGGGDGG",
           "...GGGGGGGGGGGGG",
           "...GGGGGGGGGGDDG",
           "...GGGGGGGGGGGGG",
           "...GGGGGMMMMMMMM",
           "...GGGGMTMTMTMTM",
           "...GGGGMMMMMMMMM",
           "...GGGGGGGGGGGGG",
           "....GGGGGGGGGGGG",
           "....GGGGGGGGGGGG",
           ".....GGGGGGGGGGG",
           "......GGGGGGGGGG",
           ".......GGGGGGGGG",
           ".........GGGGGGG"],0,2,mir=True)
    apply(c,m,dict(K='3a3a40',G='50b030',B='1e5a12',D='2e7a1c',W='f0f0e0',M='14300c',T='f0f0e0'))
    c.fill(edge(m['K'])-dilate(m['G']),'5a5a64'); c.fill(edge(m['G'])-dilate(m['K']),'2e7a1c')

@icon('marvel_hawkeye_target')
def _(c):
    for r,col in [(13.6,'7a2ab0'),(11,'e8e8e8'),(8.4,'7a2ab0'),(5.8,'e8e8e8'),(3.2,'7a2ab0')]: c.fill(disc(13,19,r),col)
    c.fill(line(13,19,29,3,1.6),'a0a8b0'); c.fill(line(13.5,18.5,29,3),'d8dce0')
    c.fill(poly([(25,3),(29,3),(29,7),(31,5),(31,1),(27,1)]),'b050ff'); c.fill(poly([(27,1),(31,1),(31,5)]),'7a2ab0')

@icon('marvel_venom')
def _(c):
    head=ell(16,15,13,15); c.fill(head,'101018')
    c.fill(edge(head)&{(x,y) for x,y in head if y<16},'3a4a8a'); c.fill(line(8,4,5,10)|line(24,4,27,10),'3a4a8a')
    eyes=mirror(poly([(2.5,7),(9,6),(14.5,12.5),(13.5,15),(7,13)])); c.fill(eyes,'f4f4f4')
    mouth=ell(16,23,11,5.2)&head; c.fill(mouth,'4a0818')
    teeth=set()
    for x in range(6,27,2):
        teeth|=poly([(x-1,17.6),(x+1,17.6),(x,21.5)])|poly([(x-1,28.4),(x+1,28.4),(x,24.5)])
    c.fill(teeth&mouth,'f4f4f4')
    c.fill(poly([(14,24),(18,24),(17,31.5),(12,31.5),(15,28)]),'e0306a')

@icon('marvel_daredevil')
def _(c):
    m=art(["................",
           "......H.........",
           "......HH........",
           "......HHR.......",
           ".....RHRRRRRRRRR",
           "....RRRRRRRRRRRR",
           "...RRRRRRRRRRRRR",
           "...RRRRRRRRRRRRR",
           "..RRRRRRRRRRRRRR",
           "..RRRRRRRRRRRRRR",
           "..RRRRRRRRRRRRRR",
           "..RRRDDDRRRRRRRR",
           "..RRRRDDDDRRRRRR",
           "..RRRRRDDDDRRRRR",
           "..RRRRRRRRRRRRRR",
           "..RRRRRRRRRRRRRR",
           "..RRRRRRRRRRRRRR",
           "..RRRSSSSSSSSSSS",
           "..RRRSSSSSSSSSSS",
           "..RRRSSSSSSSSSSS",
           "...RRSSSSSSSSSSS",
           "...RRSSSSSSSMMMM",
           "...RRSSSSSSSSSSS",
           "....RSSSSSSSSSSS",
           "....RRSSSSSSSSSS",
           ".....RRSSSSSSSSS",
           "......RRSSSSSSSS",
           "........RRSSSSSS",
           "..........RRSSSS"],0,1,mir=True)
    apply(c,m,dict(H='e02020',R='b01010',D='3a0404',S='e0a878',M='8a4a30'))
    c.fill(edge(m['R'])-dilate(m['S']),'6a0808'); c.fill(line(16,4,16,16),'8a0c0c')

# ───────────── Masters of the Universe ─────────────
@icon('motu_power_sword')
def _(c):
    blade=poly([(13.5,22),(18.5,22),(18.5,5),(16,0.5),(13.5,5)])
    c.fill(dilate(blade)-blade,'0a2440'); c.fill(blade,'c8d0d8'); c.fill({(x,y) for x,y in blade if x<=14},'eef4ff'); c.fill({(x,y) for x,y in blade if x>=18},'8890a0')
    c.fill(rect(16,3,16,21),'a0a8b4')
    guard=rect(7,20,24,22)|poly([(4,16),(8,20),(8,23),(5,21)])|poly([(28,16),(24,20),(24,23),(27,21)])
    c.fill(guard,'d8a020'); c.fill(rect(7,22,24,22),'8a6010'); c.fill(disc(16,21,2.2),'a040c0')
    c.fill(rect(14,23,17,28),'6a3a8a');
    for y in (24,26,28): c.fill(rect(14,y,17,y),'4a2a60')
    c.fill(disc(16,29.8,2.1),'d8a020'); c.put(15,29,'ffe080')

@icon('motu_castle_grayskull')
def _(c):
    vgrad(c,'1a0a2a','3a1a40')
    for x,y in [(3,3),(10,1),(27,2),(21,0),(6,8)]: c.put(x,y,'a090c0')
    c.fill(rect(0,29,31,31),'2a2a20')
    c.fill(rect(1,9,7,28),'6a7a6a'); c.fill(ell(4,9,3.6,3.5),'8a5aa0'); c.fill(rect(4,4,4,6),'8a5aa0')
    c.fill(rect(24,6,30,28),'5a6a5a'); c.fill({(x,5) for x in range(24,31,2)},'5a6a5a'); c.fill(rect(29,1,29,5),'807060'); c.fill(rect(30,1,31,2),'c02020')
    c.fill(rect(25,12,25,14)|rect(28,18,28,20)|rect(3,14,3,16),'101010')
    skull=ell(16,14,9.5,9)|rect(8,14,24,28); c.fill(skull,'7a9a7a'); c.fill(edge(skull),'4a6a4a')
    c.fill({(x,y) for x,y in skull if x>=21},'628262')
    c.fill(ell(12,14,2.6,2.2)|ell(20,14,2.6,2.2),'0a0a0a'); c.put(12,14,'e04020'); c.put(20,14,'e04020')
    c.fill(poly([(16,16),(17.6,19.5),(14.4,19.5)]),'1a2a1a')
    c.fill(rect(10,21,21,28),'2a1a0a')
    for x in range(10,22,2): c.fill({(x,21),(x,22)},'d8d8c0')
    c.fill(rect(11,24,20,28),'6a4a20')
    for x in (13,16,19): c.fill(rect(x,24,x,28),'4a3010')

@icon('motu_skeletor')
def _(c):
    m=art([".......PPPPPPPPP",
           ".....PPPPPPPPPPP",
           "....PPPPPPPPPPPP",
           "...PPPPPPRRRRRRR",
           "..PPPPPPPPPPPPPP",
           "..PPPPPPRRRRRRRR",
           "..PPPPPQYYYYYYYY",
           ".PPPPQYYYYYYYYYY",
           ".PPPPQYYYYYYYYYY",
           ".PPPQYYYYYYYYYYY",
           ".PPPQYYKKKKYYYYY",
           ".PPPQYKKKKKKYYYY",
           ".PPPQYKKEKKKYYYY",
           ".PPPQYYKKKKYYYYY",
           ".PPPQYYYYYYYYYKY",
           ".PPPPQYYYYYYYYKK",
           ".PPPPQYYYYYYYYYY",
           ".PPPPPQYYYYYYYYY",
           "..PPPPQYTKTKTKTK",
           "..PPPPPQYTKTKTKT",
           "..PPPPPQYYYYYYYY",
           "..PPPPPPQYYYYYYY",
           "..PPPPPPPQYYYYYY",
           "...PPPPPPPQQQQQQ",
           "...PPPPPPPPPPPPP",
           "..BBBPPPPPPPPPPP",
           ".BBBBBBPPPPPPPPP",
           "BBBBBBBBBKKKKKKK",
           "BBBBBBBBBKKKKKKK"],0,1,mir=True)
    apply(c,m,dict(P='7a2aa0',R='5a1a7a',Q='4a1a6a',Y='f0d040',K='0a0a0a',E='e02020',T='f8f0c0',B='3070d0'))
    c.fill(edge(m['P'])-dilate(m['Y']),'4a1a6a')
    c.fill({(x,y) for x,y in m['Y'] if x in (6,25) or (x in (7,24) and y>10)},'b89020')

@icon('motu_orko')
def _(c):
    hat=poly([(16,1),(20.5,5),(21.5,11),(10.5,11),(11.5,5)])|ell(16,11.6,10,1.6)
    c.fill(hat,'5a4ac8'); c.fill({(x,y) for x,y in hat if x>=19 and y<11},'3a2a90'); c.fill(edge(ell(16,11.6,10,1.6))&{(x,y) for x in range(32) for y in range(12,14)},'2a1a70')
    c.fill(rect(11,9,20,9),'f0c020')
    c.fill(rect(11,13,20,17)-{(11,17),(20,17)},'0a0a24')
    c.fill(rect(13,14,14,15)|rect(17,14,18,15),'ffe020')
    sc=rect(9,18,22,20)|poly([(19,19),(24,24),(21,25)]); c.fill(sc,'d02828'); c.fill(rect(9,20,22,20),'901818')
    robe=poly([(10,21),(22,21),(23,28),(19,29),(16,27.5),(13,29),(9,28)]); c.fill(robe,'5a4ac8'); c.fill({(x,y) for x,y in robe if x>=19},'3a2a90')
    c.fill(disc(16,24.5,2.8)-disc(16,24.5,1.3),'d02828')
    c.fill(disc(5.5,21,1.9)|disc(26.5,19,1.9),'f0a0a0')
    for x,y in [(4,25),(27,24),(7,28),(25,29)]: c.put(x,y,'a0a0ff')

@icon('motu_havoc_staff')
def _(c):
    vgrad(c,'0a1a10','1a0a20')
    c.fill(disc(16,11,10),'14281a'); c.fill(disc(16,11,7),'1c3a22')
    c.fill(rect(15,17,16,31),'5a3a7a'); c.fill(rect(15,17,15,31),'8a6aaa')
    for y in (21,26): c.fill(rect(14,y,17,y+1),'b0a080')
    for cx,sgn in ((8.5,-1),(23.5,1)):
        h=disc(cx,9,5)-disc(cx,9,2.4); c.fill(h-ell(16,10,5,7),'a07850'); c.fill(disc(cx,9,2.4)-disc(cx,9,1),'7a5a38')
    sk=poly([(10,4),(22,4),(21,10),(18.5,17),(13.5,17),(11,10)]); c.fill(sk,'d8d0b8'); c.fill({(x,y) for x,y in sk if x>=19},'a8a088'); c.fill(edge(sk),'6a6450')
    c.fill(ell(13,9,1.8,1.6)|ell(19,9,1.8,1.6),'0a0a0a'); c.put(13,9,'40ff40'); c.put(18,9,'40ff40')
    c.fill(rect(14,14,14,15)|rect(17,14,17,15),'6a6450')

@icon('motu_he_man_harness')
def _(c):
    body=poly([(1,32),(1,10),(6,4),(26,4),(31,10),(31,32)]); c.fill(body,'e0a070'); c.fill(edge(body),'a0684a')
    c.fill(polyline([(11,4),(16,7),(21,4)]),'a0684a')
    straps=line(5,4,16,16,3)|line(27,4,16,16,3); c.fill(straps,'8a9098'); c.fill(edge(straps),'5a6068')
    c.fill(polyline([(8,23),(12,25),(16,24),(20,25),(24,23)]),'b0785a')
    cx,cy=16,15; cr=set()
    for a in range(4):
        r=math.radians(a*90-90); d=(math.cos(r),math.sin(r)); p=(-d[1],d[0])
        P=lambda l,w:(cx+d[0]*l+p[0]*w,cy+d[1]*l+p[1]*w)
        cr|=poly([P(0,-1.4),P(7.5,-4.5),P(7.5,4.5),P(0,1.4)])
    cr|=disc(cx,cy,2)
    c.fill(outline(cr),'2a2a30'); c.fill(cr,'a8b0b8'); c.fill({(x,y) for x,y in cr if x>=17 or y>=17},'7a8088'); c.fill(disc(cx,cy,1.5),'5a6068')
    c.fill(rect(1,29,31,31),'8a5a2a'); c.fill(rect(1,29,31,29),'c08840')

@icon('motu_battle_cat')
def _(c):
    m=art(["................",
           "..EE............",
           "..EEE...........",
           "..EEEE..........",
           "..GEEEGGHHHHHHHH",
           "..GGGGGHHHHHHHHH",
           ".GGSGGHHHHHHHHHH",
           ".GGSSGHHHHHHHHHH",
           ".GGGSGHHHHKHHHHH",
           "GGSGGGGHHHHKHHHH",
           "GGSSGWWGGHHHKHHH",
           "GGGSGWPGGGHHHHHH",
           "GSGGGGGGGGGHHHHH",
           "GSSGGGGGGGGGHHHH",
           "GGSGGGGGGGGGGHHH",
           "GGGSGGGMMMMMMMHH",
           ".GGGGGMMMMMMMMNN",
           ".GGSGMMMMMMMMMNN",
           "..GGGMMMMMMMMMMM",
           "..GGGMMMLMMMMMML",
           "...GGMMMMLLLLLLL",
           "...GGGMMMTLLLLLL",
           "....GGGMMTLLLLLL",
           ".....GGGMMMMMMMM",
           "......GGGGMMMMMM",
           "........GGGGGGGG"],0,3,mir=True)
    apply(c,m,dict(E='2a7a2a',G='40a840',S='f0b020',H='c02020',K='701010',W='f0e020',P='101010',M='c8e0a0',N='2a1a10',L='4a1010',T='f8f8f8'))
    c.fill(edge(m['H'])-dilate(m['M']),'801010')

@icon('motu_snake_mountain')
def _(c):
    vgrad(c,'200818','4a1010')
    c.fill(polyline([(3,0),(6,5),(4,7),(8,12)]),'ffe060')
    mt=poly([(0,32),(3,20),(8,13),(12,9),(16,3),(19,8),(24,12),(28,19),(32,32)]); c.fill(mt,'3a3048')
    c.fill({(x,y) for x,y in mt if x<15 and (x,y-1) not in mt or (x-1,y) not in mt and x<16},'5a4a6a')
    c.fill(ell(13,22,2.4,2)|ell(19,22,2.4,2),'0a0a0a'); c.fill(poly([(13,26),(19,26),(16,31.5)]),'0a0a0a')
    sn=polyline([(30,31),(22,28),(9,27),(6,23),(10,19),(21,17),(24,14),(20,11),(14,10),(13,7),(16,5)],2.6)
    c.fill(outline(sn),'0a1a0a'); c.fill(sn,'40a030'); c.fill(polyline([(30,31),(22,28),(9,27)])|polyline([(10,19),(21,17)]),'a0d040')
    hd=ell(17,3.5,3.4,2.4); c.fill(hd,'40a030'); c.fill(edge(hd),'1a5a10'); c.put(16,3,'ff2020'); c.put(19,3,'ff2020')
    c.fill({(20,5),(20,6),(21,6)},'e02020')

@icon('motu_i_have_the_power')
def _(c):
    vgrad(c,'1a0a2a','3a1a10')
    for s,y,col in [("I HAVE",4,'ffe080'),("THE",13,'ffd040'),("POWER!",22,'ffb020')]:
        t=txt(s,y); c.fill({(x+1,y2+1) for x,y2 in t},'5a2a08'); c.fill(t,col)
    c.fill(polyline([(2,12),(5,15),(3,17),(6,20)]),'a0e0ff'); c.fill(polyline([(29,12),(26,15),(28,17),(25,20)]),'a0e0ff')

@icon('motu_by_the_power')
def _(c):
    vgrad(c,'100828','2a1040')
    random.seed(11)
    for (x0,y0,x1,y1) in [(16,1,2,6),(16,1,30,7),(16,1,3,18),(16,1,29,19),(16,1,8,0),(16,1,25,0)]:
        pts=[(x0,y0)]
        for i in range(1,5):
            t=i/5; pts.append((x0+(x1-x0)*t+random.uniform(-2,2),y0+(y1-y0)*t+random.uniform(-2,2)))
        pts.append((x1,y1)); c.fill(polyline(pts),'fff080')
    c.fill(dilate(rect(15,2,16,16),1),'4a80c0'); c.fill(rect(15,2,16,16),'e8f4ff'); c.fill(rect(16,3,16,16),'a8b8c8')
    c.fill(rect(11,17,20,18),'d8a020'); c.fill(rect(11,18,20,18),'8a6010')
    fist=ell(16,21.5,3.6,3)|rect(13,23,18,31); c.fill(fist,'e0a070'); c.fill({(x,y) for x,y in fist if x>=17},'b07a50')
    c.fill({(13,20),(13,22),(14,21)},'a0684a'); c.fill(rect(12,26,19,29),'8a9098'); c.fill(rect(12,26,19,26),'c0c8d0')

for name,f in icons.items():
    c=C(); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
