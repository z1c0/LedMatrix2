import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, png
from draw import *
OUT=sys.argv[1]; icons={}
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco
def sprite(c,rows,cols,mir=False,x0=0,y0=0):
    for r,row in enumerate(rows):
        if mir: row=row+row[::-1]
        for k,ch in enumerate(row):
            if ch!='.' and ch in cols:
                for dx in (0,1):
                    for dy in (0,1): c.put(x0+k*2+dx,y0+r*2+dy,cols[ch])
def vgrad(c,top,bot):
    t=hx(top); b=hx(bot)
    for y in range(32): c.p[y]=[tuple(round(t[i]+(b[i]-t[i])*y/31) for i in range(3))+(255,) for _ in range(32)]

# ── hardware ──
@icon('nes_console')
def _(c):
    c.fill(rect(2,7,29,12),'a8a8a8'); [c.fill(rect(3,y,28,y),'888888') for y in (8,10)]
    c.fill(rect(1,13,30,17),'d0d0d0'); c.fill(rect(7,13,24,17),'b8b8b8'); c.fill(rect(7,17,24,17),'909090')
    c.fill(rect(1,18,30,25),'3a3a3a'); c.fill(rect(1,18,30,18),'202020')
    c.put(3,20,'ff2010'); c.fill(rect(5,21,8,22)|rect(10,21,13,22),'b8b8b8')
    for x in (18,24): c.fill(rect(x,20,x+4,24),'a8a8a8'); c.fill(rect(x+1,21,x+3,23),'101010')
    c.fill(rect(2,26,5,27)|rect(26,26,29,27),'505050')

@icon('nes_controller')
def _(c):
    c.fill(polyline([(25,9),(26,4),(31,1)],1.6),'303030')
    c.fill(rrect(1,9,30,23),'c8c8c8'); c.fill(rect(2,10,29,22),'1c1c1c')
    c.fill(rect(4,15,10,17)|rect(6,13,8,19),'4a4a4a'); c.put(7,16,'303030')
    for y in (12,15,18): c.fill(rect(12,y,19,y+1),'a0a0a0')
    c.fill(rect(12,21,14,21)|rect(17,21,19,21),'101010')
    for x in (22,27): c.fill(disc(x,17,2.3),'d82800'); c.put(x-1,16,'ff8060')
    c.fill(rect(20,20,29,20),'a0a0a0')
def rrect(x0,y0,x1,y1): return rect(x0,y0,x1,y1)-{(x0,y0),(x1,y0),(x0,y1),(x1,y1)}

@icon('nes_cartridge')
def _(c):
    c.fill(rect(5,2,26,29),'a8a8a8'); c.fill(rect(25,2,26,29),'808080'); c.fill(rect(5,2,6,29),'c8c8c8')
    for y in range(3,9,2): c.fill(rect(8,y,23,y),'7a7a7a')
    c.fill(rect(8,11,23,25),'202020'); c.fill(rect(9,12,22,19),'5c94fc'); c.fill(rect(9,20,22,24),'c84c0c')
    c.fill(rect(9,20,22,20),'00a800'); c.fill(disc(13,16,2)|rect(12,16,14,19),'00a800'); c.fill(rect(18,14,20,16),'f8b800')
    c.fill(rect(5,28,26,29),'707070')

@icon('nes_zapper')
def _(c):
    O,L,D='f87800','fcb050','a84800'
    g=rect(4,9,22,14)|rect(18,7,26,14)|poly([(17,14),(25,14),(29,28),(21,29)])
    c.fill(outline(g),'3a1800'); c.fill(g,O); c.fill(rect(4,9,22,9)|rect(18,7,26,7),L); c.fill({(x,y) for x,y in g if y>=14 and x>=26},D)
    c.fill(rect(1,8,3,15),'303030')
    c.fill(polyline([(17,15),(15,20),(19,22),(21,20)]),'3a1800'); c.fill(rect(18,16,18,19),'303030')
    c.fill(rect(4,14,16,14),D)

# ── Super Mario ──
MUSH=[".....KKK","...KKWWR","..KRRWWW",".KRRRWWR",".KWWRRRR","KWWWWRRR","KWWWWRRR","KWWWRRRR","KRRRRRRR",".KKKKKKK","..KSSSKS","..KSSSKS","..KSSSSS","..KSSSSS","...KSSSS","....KKKK"]
@icon('mario_mushroom')
def _(c): sprite(c,MUSH,dict(K='000000',R='d82800',W='fcfcfc',S='fcd8a8'),True)
@icon('mario_1up')
def _(c): sprite(c,MUSH,dict(K='000000',R='00a800',W='fcfcfc',S='fcd8a8'),True)
@icon('mario_fire_flower')
def _(c): sprite(c,["...KKKKK",".KKRRRRR","KRROOOOO","KROOYYYY","KROOYYWW","KRROOOOO",".KKRRRRR","...KKKKK",".......G",".GG....G","GGGG...G","GGGGG..G",".GGGGG.G","..GGGGGG","....GGGG",".......G"],dict(K='000000',R='d82800',O='fc9838',Y='f8d878',W='fcfcfc',G='00a800'),True)
@icon('mario_starman')
def _(c): sprite(c,[".......K","......KY","......KY",".....KYY","KKKKKKYY","KYYYYYYY",".KYYYYKY","..KYYYKY","...KYYKY","...KYYYY","..KYYYYY","..KYYYYK",".KYYYKK.",".KYKK...",".KK.....","........"],dict(K='883000',Y='f8b800'),True)
@icon('mario_question_block')
def _(c): sprite(c,[".OOOOOOOOOOOOOO.","OYYYYYYYYYYYYYYK","OYKYYYYYYYYYYKYK","OYYYYWWWWWYYYYYK","OYYYWWKKKWWYYYYK","OYYYWWKYYWWKYYYK","OYYYWWKYYWWKYYYK","OYYYYKKYWWWKYYYK","OYYYYYYWWKKKYYYK","OYYYYYYWWKYYYYYK","OYYYYYYYKKYYYYYK","OYYYYYYWWYYYYYYK","OYYYYYYWWKYYYYYK","OYKYYYYYKKYYYYKK","OYYYYYYYYYYYYYYK","KKKKKKKKKKKKKKKK"],dict(O='fcd8a8',Y='e09018',W='fcfcfc',K='883000'))
@icon('mario_coin')
def _(c):
    e=ell(16,16,8.5,13.5); c.fill(e,'f8b800'); c.fill(edge(e),'883000'); c.fill(ell(16,16,5,10)-ell(16,16,3,8),'fcd878')
    c.fill(rect(15,9,16,22),'c88800'); c.fill(rect(10,8,10,12),'fcfcfc')
@icon('mario_goomba')
def _(c): sprite(c,["......KK",".....KBB","....KBBB","...KBBBB","..KBKKBB",".KBBBKKB",".KBBWKWB","KBBBWKWB","KBBBWWWB","KBBBBBBB",".KBBBTTT","..KKTTTT","...KTTTT",".FFFKTTK","FFFFFFK.",".FFFFF.."],dict(K='3a1a08',B='a85010',W='fcd8a8',T='fcd8a8',F='4a2a10'),True)
@icon('mario_koopa_shell')
def _(c): sprite(c,["........","........","........",".....KKK","...KKGGG","..KGGGLG",".KGGLLGG",".KGLLGGL","KGGGGGLL","KGGGGGGG","KWWWWWWW","KYYYYYYY",".KKKKKKK","........","........","........"],dict(K='000000',G='00a800',L='80d010',W='fcfcfc',Y='f8d878'),True,0,2)
@icon('mario_warp_pipe')
def _(c):
    rim=rect(3,4,28,11); body=rect(5,12,26,31)
    for m in (rim,body):
        c.fill(m,'00a800'); c.fill({(x,y) for x,y in m if x<=8},'80d010'); c.fill({(x,y) for x,y in m if x>=22},'005800'); c.fill(edge(m),'000000')
    c.fill(rect(10,5,11,10)|rect(10,13,11,30),'b8f818')
@icon('mario_flagpole')
def _(c):
    c.fill(rect(0,28,31,31),'c84c0c'); c.fill(rect(0,28,31,28),'fcd8a8'); c.fill(rect(15,24,24,27),'c84c0c'); c.fill(edge(rect(15,24,24,27)),'000000')
    c.fill(rect(19,4,20,23),'80d010'); c.fill(disc(19.5,3,2.4),'00a800'); c.put(19,2,'b8f818')
    f=poly([(18.5,5),(18.5,13),(7,5)]); c.fill(f,'fcfcfc'); c.fill(disc(15,7,1.6),'00a800')
@icon('mario_super_leaf')
def _(c): sprite(c,["...........K....","..........KK....","........KKOK....","......KKOOOK....",".....KOOOOOOK...","....KOOLOOOOOK..","...KOOLLKOOOOK..","..KOOLLKOOOOOK..","..KOLLKOOOOOK...",".KOLLKOOOOOOK...",".KOLKOOOOOOK....",".KOKOOOOOKK.....","..KOOOOKK.......","..KKKKK.........",".K..............","K..............."],dict(K='4a2008',O='e07020',L='f8b040'))
@icon('mario_bullet_bill')
def _(c):
    body=(ell(14,16,12,10.5)&rect(0,0,14,31))|rect(14,6,24,26)
    c.fill(body,'202028'); c.fill(edge(body),'6a6a80'); c.fill(rect(24,4,27,28),'101018'); c.fill(edge(rect(24,4,27,28)),'50506a'); c.fill(rect(28,9,30,23),'505060')
    c.fill(ell(10,13,3,3.4),'fcfcfc'); c.fill(rect(8,12,9,14),'000000'); c.fill(line(6,8,13,10),'fcfcfc')
    c.fill(disc(17,21,3),'fcfcfc'); c.fill(rect(15,21,19,21),'a0a0a0')

# ── Zelda ──
@icon('zelda_triforce')
def _(c):
    t=poly([(16,2),(30.5,28),(1.5,28)])-poly([(9,15.5),(23,15.5),(16,28.5)])
    c.fill(t,'f8b800'); c.fill({(x,y) for x,y in t if x+0.5*y>=31 or y>=26},'c88800'); c.fill(edge(t)&{(x,y) for x,y in t if x+0.5*y<26 and y<26},'fcfc78')
@icon('zelda_sword')
def _(c):
    b=poly([(14,21),(18,21),(18,5),(16,1.5),(14,5)]); c.fill(b,'fcfcfc'); c.fill({(x,y) for x,y in b if x>=16},'bcbcbc')
    c.fill(rect(9,21,22,23),'c84c0c'); c.fill(rect(9,21,22,21),'fc9838'); c.fill(rect(14,24,17,28),'883000'); c.fill(rect(13,29,18,30),'c84c0c')
@icon('zelda_heart_container')
def _(c):
    h=disc(10.5,11,6.2)|disc(21.5,11,6.2)|poly([(4.6,13),(27.4,13),(16,28)]); c.fill(outline(h),'581000'); c.fill(h,'d82800')
    c.fill({(x,y) for x,y in h if x+y>36},'a81000'); c.fill(rect(8,8,9,9)|{(10,8),(8,10)},'fcfcfc')
@icon('zelda_rupee')
def _(c):
    r=poly([(16,1.5),(24,8),(24,24),(16,30.5),(8,24),(8,8)]); c.fill(outline(r),'003800'); c.fill(r,'00a800')
    c.fill(poly([(16,6),(20,9),(20,23),(16,26),(12,23),(12,9)]),'80d010'); c.fill({(x,y) for x,y in r if x>=21},'005800'); c.fill(rect(13,10,13,16),'d8f878')
@icon('zelda_boomerang')
def _(c):
    b=polyline([(6,5),(11,23),(27,24)],4.5); c.fill(outline(b),'3a1800'); c.fill(b,'c84c0c'); c.fill(polyline([(5,5),(10,22),(26,22.5)]),'fc9838')

# ── Metroid ──
@icon('metroid')
def _(c):
    d=ell(16,13,13.5,10.5); c.fill(d,'80d010'); c.fill(edge(d),'005800'); c.fill(ell(16,15,11,6)&d,'58b028')
    for x,y in ((10,14),(16,11),(22,14)): c.fill(disc(x,y,2.9),'d82800'); c.fill(disc(x,y,1.2),'f87858')
    c.fill(rect(7,6,10,7),'d8f8b8')
    for x in (8,13,19,24): c.fill(poly([(x-2,22),(x+2,22),(x-0.5,30)]),'fcd8a8'); c.fill(line(x+1,22,x,28),'c89868')
@icon('samus_helmet')
def _(c):
    h=ell(16,16,12,13.5); c.fill(h,'d84000'); c.fill({(x,y) for x,y in h if y<12},'f87800'); c.fill(edge(h),'581000')
    v=poly([(7,13),(25,13),(23,22),(9,22)]); c.fill(outline(v),'202020'); c.fill(v,'00a800'); c.fill(poly([(9,14),(14,14),(11,20)]),'b8f818')
    c.fill(disc(4.5,17,2.5)|disc(27.5,17,2.5),'f8b800'); c.fill(rect(12,24,19,26),'f8b800'); c.fill(rect(15,4,16,10),'fc9838')

# ── Mega Man ──
@icon('megaman_helmet')
def _(c): sprite(c,[".....KKK","...KKBBB","..KBBBLL",".KBBBBLL",".KBBBSSS","KBBBSSSS","KLLBSWWS","KLLBSWKS","KLLBSWKS","KBBBSSSS",".KBBSSSS",".KBBSSSS","..KBBSSK","...KBBSS","....KKKK","........"],dict(K='000000',B='0070ec',L='3cbcfc',S='fcd8a8',W='fcfcfc'),True,0,1)
@icon('megaman_e_tank')
def _(c):
    c.fill(rect(10,3,21,6),'3cbcfc'); c.fill(rect(10,3,21,3),'a8e8fc')
    t=rrect(5,6,26,28); c.fill(t,'0070ec'); c.fill(edge(t),'002888'); c.fill(rect(7,8,24,26),'0058c8')
    E=rect(11,10,20,12)|rect(11,10,13,24)|rect(11,16,18,18)|rect(11,22,20,24); c.fill(E,'fcfcfc')
    c.fill(line(22,9,24,11),'a8e8fc')

# ── assorted ──
@icon('duck_hunt_duck')
def _(c):
    vgrad(c,'3cbcfc','64c8fc'); c.fill(rect(0,27,31,31),'00a800'); c.fill(rect(0,27,31,27),'80d010')
    c.fill(poly([(4,16),(8,14),(8,20)]),'503000')
    c.fill(ell(15,18,8.5,5),'a85010'); c.fill(ell(17,20,6,2.8),'fcd8a8')
    c.fill(poly([(9,16),(13,2),(19,15)]),'6a3010'); c.fill(poly([(12,12),(14,5),(17,13)]),'a85010')
    c.fill(disc(25,12,3.6),'00a800'); c.fill(rect(22,15,26,16),'fcfcfc'); c.fill(poly([(28,11),(31.5,13),(28,14.5)]),'fc9838'); c.put(25,11,'000000')
@icon('punch_out_gloves')
def _(c):
    for cx,sg in ((9.5,1),(22.5,-1)):
        g=ell(cx,13,7,8.5)|ell(cx+sg*5.5,16,2.6,3.5); c.fill(outline(g),'003800'); c.fill(g,'00a800')
        c.fill(ell(cx-sg*2.5,9,2.5,3)&g,'58f898')
        cuff=rect(int(cx-5),21,int(cx+4),27); c.fill(cuff,'fcfcfc'); c.fill(rect(int(cx-5),24,int(cx+4),24),'bcbcbc')
@icon('kirby')
def _(c):
    c.fill(ell(9,27,6,3)|ell(23,27,6,3),'d82800')
    b=ell(16,15,12,11.5)|disc(4,17,3)|disc(28,13,3); c.fill(b,'f8a0c0'); c.fill(edge(b),'c85890')
    for x in (13,19): c.fill(ell(x,12,1.3,3),'000000'); c.put(x-1 if x==13 else x-1,10,'fcfcfc')
    c.fill(ell(9,17,2.2,1.2)|ell(23,17,2.2,1.2),'f05080'); c.fill({(15,18),(16,18),(16,17),(15,17)},'a02040')
@icon('castlevania_candle')
def _(c):
    for y in range(32):
        for x in range(32):
            c.put(x,y,'201418' if (y%6==5 or (x+(y//6)*4)%10==9) else '3a2830')
    c.fill(poly([(16,1),(19,7),(17,10),(15,10),(13,7)]),'fc9838'); c.fill(poly([(16,4),(17.5,8),(16,10),(14.5,8)]),'f8d878')
    c.fill(rect(14,10,17,22),'fcfcfc'); c.fill(rect(17,11,17,22),'bcbcbc'); c.fill(rect(15,10,15,13),'fcd8a8')
    c.fill(rect(10,22,21,24),'f8b800'); c.fill(rect(10,24,21,24),'c88800'); c.fill(rect(15,25,16,30),'c88800'); c.fill(rect(12,30,19,31),'f8b800')
@icon('konami_code')
def _(c):
    def arrow(x,y,d):
        up=poly([(3.5,0),(7,3.5),(4.5,3.5),(4.5,7),(2.5,7),(2.5,3.5),(0,3.5)])
        rot={'u':lambda a,b:(a,b),'d':lambda a,b:(a,6-b),'l':lambda a,b:(b,a),'r':lambda a,b:(6-b,a)}[d]
        return {(x+rot(a,b)[0],y+rot(a,b)[1]) for a,b in up if a<7 and b<7}
    for i,d in enumerate('uudd'): c.fill(arrow(1+i*8,2,d),'fcfcfc')
    for i,d in enumerate('lrlr'): c.fill(arrow(1+i*8,11,d),'fcfcfc')
    B=".##|#.#|##.|#.#|##."; B="##.|#.#|##.|#.#|##."; A=".#.|#.#|###|#.#|#.#"
    for gx,g in ((7,B),(19,A)):
        for r,row in enumerate(g.split('|')):
            for k,v in enumerate(row):
                if v=='#': c.fill(rect(gx+k*2,21+r*2,gx+k*2+1,22+r*2),'d82800')

for name,f in icons.items():
    c=C(); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
