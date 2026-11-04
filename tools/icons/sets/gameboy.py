import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, png
from draw import *
OUT=sys.argv[1]; icons={}
D0,D1,D2,D3='0f380f','306230','8bac0f','9bbc0f'
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco
def sprite(c,rows,cols,x0=0,y0=0):
    for r,row in enumerate(rows):
        for k,ch in enumerate(row):
            if ch in cols:
                for dx in (0,1):
                    for dy in (0,1): c.put(x0+k*2+dx,y0+r*2+dy,cols[ch])
def rrect(x0,y0,x1,y1): return rect(x0,y0,x1,y1)-{(x0,y0),(x1,y0),(x0,y1),(x1,y1)}
FONT={'N':"##.|#.#|#.#|#.#|#.#",'I':"###|.#.|.#.|.#.|###",'T':"###|.#.|.#.|.#.|.#.",'E':"###|#..|##.|#..|###",'D':"##.|#.#|#.#|#.#|##.",'O':".#.|#.#|#.#|#.#|.#.",'A':".#.|#.#|###|#.#|#.#",'B':"##.|#.#|##.|#.#|##."}
def text(s,x,y,sc=1):
    m=set()
    for ch in s:
        for r,row in enumerate(FONT[ch].split('|')):
            for k,v in enumerate(row):
                if v=='#': m|=rect(x+k*sc,y+r*sc,x+k*sc+sc-1,y+r*sc+sc-1)
        x+=4*sc
    return m

@icon('gb_console')
def _(c):
    body=rrect(6,1,25,30)-(rect(22,27,25,30)-disc(21.5,26.5,4.2)); c.fill(body,D2); c.fill(edge(body),D0)
    c.fill(rect(8,3,23,14),D1); c.fill(rect(11,5,20,12),D3); c.fill(rect(12,10,19,11)|rect(14,7,15,9),D1); c.put(9,6,D0)
    c.fill(rect(8,19,12,19)|rect(10,17,10,21),D0)
    c.fill(disc(18.5,21,1.6)|disc(22,19,1.6),D0)
    c.fill(rect(12,25,13,25)|rect(16,25,17,25),D1)
    for i in range(4): c.fill(line(18+i*2,29,21+i*2,25)&body,D1)

@icon('gb_cartridge')
def _(c):
    cart=rect(7,2,24,29)-rect(21,2,24,4)|rect(21,5,24,5)
    cart=(rect(7,2,24,29)-poly([(20.5,1.5),(25,1.5),(25,6)])); c.fill(cart,D2); c.fill(edge(cart),D0)
    for y in (4,6): c.fill(rect(9,y,18,y),D1)
    c.fill(rect(9,10,22,24),D0); c.fill(rect(10,11,21,23),D3)
    c.fill(rect(11,18,20,22),D1); c.fill(rect(13,14,16,17),D1); c.fill(poly([(14.5,12),(17,15),(12,15)]),D0)
    c.fill(poly([(14,26),(18,26),(16,28)]),D1)

@icon('gb_link_cable')
def _(c):
    for x0 in (3,21):
        p=rrect(x0,3,x0+7,11); c.fill(p,D1); c.fill(edge(p),D0); c.fill(rect(x0+2,1,x0+5,3),D0); c.fill(rect(x0+2,5,x0+5,8),D2)
    cab=polyline([(6.5,11),(6,18),(9,24),(16,26),(23,24),(26,18),(25.5,11)],2.2); c.fill(cab,D0)
    c.fill(polyline([(13,26),(16,23),(19,26),(16,29),(13,26)]),D1)

@icon('gb_buttons')
def _(c):
    c.fill(rect(0,0,31,31),D2)
    for cx,cy,l in ((9,17,'B'),(23,11,'A')):
        c.fill(disc(cx,cy,5.6),D1); c.fill(disc(cx,cy,5),D0); c.fill(text(l,cx-1,cy-2),D2)
    c.fill(line(7,28,11,25,2)|line(17,28,21,25,2),D1)

@icon('gb_speaker')
def _(c):
    c.fill(rect(0,0,31,31),D2)
    for i in range(6): c.fill(line(2+i*5,30,12+i*5,2,2.2),D1)
    c.fill(rect(0,0,31,0)|rect(0,31,31,31),D0)

@icon('gb_nintendo_boot')
def _(c):
    c.fill(text("NINTENDO",0,13),D0)
    c.fill(rect(28,8,30,10)-{(29,9)},D1)

@icon('gb_batteries')
def _(c):
    for i in range(4):
        x=2+i*7; b=rrect(x,6,x+5,29); c.fill(b,D1); c.fill(edge(b),D0)
        c.fill(rect(x+2,4,x+3,5),D0); c.fill(rect(x+1,14,x+4,21),D2); c.fill(rect(x+2,17,x+3,17)|rect(x+2.5 if False else x+2,16,x+2,18),D0)

@icon('gb_tetris_stack')
def _(c):
    c.fill(rect(0,0,31,31),D3)
    grid=["........","...TTT..","....T...","........","BB......","BBL...SS","ILL..SSO","ILJZZ.OO"]
    for r,row in enumerate(grid):
        for k,ch in enumerate(row):
            if ch=='.': continue
            x,y=k*4,r*4
            if ch=='#':
                c.fill(rect(x,y,x+3,y+3),D3); continue
            fill=D2 if ch in 'TLZO' else D1
            c.fill(rect(x,y,x+3,y+3),D0); c.fill(rect(x+1,y+1,x+2,y+2),fill)

@icon('gb_tetris_rocket')
def _(c):
    c.fill(rect(0,0,31,31),D3)
    r=poly([(16,1),(19.5,7),(19.5,21),(12.5,21),(12.5,7)]); c.fill(r,D2); c.fill(edge(r),D0)
    c.fill(poly([(12.5,15),(8,22),(12.5,21)])|poly([(19.5,15),(24,22),(19.5,21)]),D1)
    c.fill(disc(16,11,1.8),D1); c.fill(rect(14,22,17,23),D0)
    for x,y,r_ in ((16,26,3.4),(10,28,3.2),(22,28,3.2),(5,30,2.8),(27,30,2.8)): c.fill(disc(x,y,r_),D1); c.fill(disc(x-1,y-1,r_-1.6),D2)
    c.fill(rect(0,31,31,31),D1)

@icon('gb_music_notes')
def _(c):
    c.fill(rect(0,0,31,31),D3)
    for x,y in ((6,23),(16,20)): c.fill(ell(x,y,3.4,2.5),D0)
    c.fill(rect(9,7,10,22)|rect(19,4,20,19),D0); c.fill(poly([(9,7),(20.5,4),(20.5,7),(9,10)]),D0)
    c.fill(ell(25,27,2.6,2),D1); c.fill(rect(27,16,27,26),D1); c.fill(polyline([(27,16),(30,19),(30,22)]),D1)

@icon('gb_pokeball')
def _(c):
    c.fill(rect(0,0,31,31),D3)
    b=disc(16,16,13.6); c.fill(b,D3); c.fill({(x,y) for x,y in b if y<15},D1); c.fill(rect(2,15,29,17)&b,D0); c.fill(edge(b),D0)
    c.fill(disc(16,16,4.6),D0); c.fill(disc(16,16,2.8),D3); c.fill(rect(8,6,10,7),D2)

@icon('gb_pikachu')
def _(c):
    c.fill(rect(0,0,31,31),D3)
    ears=poly([(9,14),(2,1),(12,9)])|poly([(23,14),(30,1),(20,9)])
    c.fill(ears,D2); c.fill(poly([(2,1),(5,4.5),(7,3)])|poly([(30,1),(27,4.5),(25,3)]),D0)
    h=ell(16,19,12,10.5); c.fill(h,D2); c.fill(edge(h|ears),D0)
    for x in (11,21): c.fill(disc(x,16,2.1),D0); c.put(x-1,15,D3)
    c.fill(disc(7.5,22,2.4)|disc(24.5,22,2.4),D1)
    c.put(16,19,D0); c.fill(polyline([(13,22),(14.5,23.5),(16,22),(17.5,23.5),(19,22)]),D0)

@icon('gb_kirby')
def _(c):
    c.fill(rect(0,0,31,31),D2)
    c.fill(ell(9,27,6,3)|ell(23,27,6,3),D1); c.fill(edge(ell(9,27,6,3)|ell(23,27,6,3)),D0)
    b=ell(16,15,12,11.5)|disc(4,17,3)|disc(28,13,3); c.fill(b,D3); c.fill(edge(b),D0)
    for x in (13,19): c.fill(ell(x,12,1.3,3),D0); c.put(x-1,10,D3)
    c.fill(ell(9,17,2.2,1.2)|ell(23,17,2.2,1.2),D2); c.fill(rect(15,17,16,18),D1)

@icon('gb_metroid')
def _(c):
    c.fill(rect(0,0,31,31),D3)
    d=ell(16,13,13.5,10.5); c.fill(d,D2); c.fill(edge(d),D0); c.fill(ell(16,15,11,6)&d,D1)
    for x,y in ((10,14),(16,11),(22,14)): c.fill(disc(x,y,2.9),D0); c.fill(disc(x-0.6,y-0.6,1.1),D2)
    c.fill(rect(7,6,10,7),D3)
    for x in (8,13,19,24): c.fill(poly([(x-2,22),(x+2,22),(x-0.5,30)]),D0)

@icon('gb_dpad')
def _(c):
    c.fill(rect(0,0,31,31),D2)
    p=rect(11,3,20,28)|rect(3,11,28,20); c.fill(dilate(p),D1); c.fill(p,D0)
    c.fill(disc(16,16,2.4),D1)
    for tri in (poly([(16,5),(18.5,8),(13.5,8)]),poly([(16,26),(18.5,23),(13.5,23)]),poly([(5,15.5),(8,13),(8,18)]),poly([(27,15.5),(24,13),(24,18)])): c.fill(tri,D1)

for name,f in icons.items():
    c=C(D3); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
