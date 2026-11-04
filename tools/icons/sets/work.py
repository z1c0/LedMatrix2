import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, png
from draw import *
OUT=sys.argv[1]; icons={}
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco
def rrect(x0,y0,x1,y1): return rect(x0,y0,x1,y1)-{(x0,y0),(x1,y0),(x0,y1),(x1,y1)}
def pix(c,rows,cols,x0=0,y0=0,s=1):
    for r,row in enumerate(rows):
        for k,ch in enumerate(row):
            if ch in cols: c.fill(rect(x0+k*s,y0+r*s,x0+k*s+s-1,y0+r*s+s-1),cols[ch])
def person(c,cx,top,head,body,r=3):
    c.fill(disc(cx,top+r,r),head); c.fill(ell(cx,top+2*r+5,r+2.5,4)&rect(0,0,31,top+2*r+5),body)
AL='b8bcc4'; ALD='80848c'; SCR='101828'

@icon('work_macbook')
def _(c):
    lid=rrect(4,4,27,22); c.fill(lid,ALD); c.fill(rect(5,5,26,21),'000000')
    c.fill(rect(6,6,25,20),'1a3a8a'); c.fill(every(lambda x,y:(x-6)**2/400+(y-21)**2/120<=1)&rect(6,6,25,20),'d05080')
    c.fill(every(lambda x,y:(x-26)**2/300+(y-21)**2/90<=1)&rect(6,6,25,20),'f0a040')
    c.fill(rect(1,23,30,24),AL); c.fill(rect(2,25,29,25),ALD); c.fill(rect(13,23,18,23),ALD)

@icon('work_cmd_key')
def _(c):
    k=rrect(3,3,28,28); c.fill(k,'d8dce0'); c.fill(edge(k),'90949c'); c.fill(rect(4,26,27,27),'a8acb4')
    loop=disc(10,10,3.2)-disc(10,10,1.3)
    m=loop|{(31-x,y) for x,y in loop}; m=m|{(x,31-y) for x,y in m}
    m|=rect(10,9,21,10)|rect(10,21,21,22)|rect(9,10,10,21)|rect(21,10,22,21)
    m-=rect(12,12,19,19); c.fill(m,'202428')

@icon('work_coffee')
def _(c):
    cup=rect(6,12,21,27)-{(6,27),(21,27)}; c.fill(cup,'f0f0f0'); c.fill(rect(18,12,21,27)&cup,'c8c8d0')
    h=disc(23,19,5)-disc(23,19,2.6)-rect(0,0,21,31); c.fill(h,'c8c8d0')
    c.fill(rect(7,12,20,13),'5a3418'); c.fill(rect(9,18,17,22),'1a5ad0')
    c.fill(rect(3,28,26,29),'a0a0a8')
    for x in (10,15): c.fill(polyline([(x,10),(x-1,8),(x+1,6),(x,3)]),'a0a0a0')

@icon('work_video_call')
def _(c):
    c.fill(rrect(0,1,31,30),'303438')
    tiles=[(1,2),(16,2),(1,16),(16,16)]; heads=['e8b088','a86a40','f0c8a0','6a4028']; shirts=['2a6ad0','d04040','40a060','e0a020']
    for (x,y),h,s in zip(tiles,heads,shirts):
        c.fill(rect(x,y,x+14,y+12),'4a5260'); c.fill(disc(x+7.5,y+5,2.8),h); c.fill(ell(x+7.5,y+13,5,3.5)&rect(x,y,x+14,y+12),s)
    c.fill(edge(rect(1,2,15,14)),'40e040')

@icon('work_headset')
def _(c):
    band=(disc(16,16,12)-disc(16,16,9.5))&rect(0,0,31,16); c.fill(band,'303438'); c.fill(edge(band)&rect(0,0,31,6),'5a6068')
    for x in (3,23): c.fill(rrect(x,14,x+5,24),'202428'); c.fill(rect(x+1,15,x+4,23),'404650')
    c.fill(polyline([(6,24),(8,28),(14,29)],1.6),'303438'); c.fill(rect(14,28,17,30),'e02020')

@icon('work_one_on_one')
def _(c):
    person(c,8,10,'e8b088','2a6ad0'); person(c,24,10,'c08a60','e08020')
    b=rrect(1,0,13,7); c.fill(b,'f0f0f0'); c.fill(poly([(4,7),(8,7),(5,10)]),'f0f0f0')
    for x in (4,7,10): c.fill(rect(x,3,x+1,4),'303438')
    b=rrect(18,1,30,8); c.fill(b,'40a0ff'); c.fill(poly([(24,8),(28,8),(27,11)]),'40a0ff')
    for x in (21,24,27): c.fill(rect(x,4,x+1,5),'f0f0f0')
    c.fill(rect(0,28,31,31),'8a5a30'); c.fill(rect(0,28,31,28),'b07a48')

@icon('work_kanban')
def _(c):
    c.fill(rrect(0,0,31,31),'e8e8e8')
    for x in (10,21): c.fill(rect(x,2,x,29),'b0b0b0')
    for x,col in ((1,'d04040'),(11,'e0a020'),(22,'40a060')): c.fill(rect(x+1,2,x+7,3),col)
    cards={1:[6,11,16,21],11:[6,11],22:[6,11,16]}
    cols={1:'f8e070',11:'80c0f8',22:'90e090'}
    for x,ys in cards.items():
        for y in ys: c.fill(rect(x+1,y,x+7,y+3),cols[x]); c.fill(rect(x+2,y+1,x+5,y+1),'707070')

@icon('work_dashboard')
def _(c):
    c.fill(rrect(0,0,31,31),'14181e')
    for p in (rect(1,1,15,13),rect(16,1,30,13),rect(1,15,30,30)): c.fill(p,'222a34')
    d=disc(8,9,5.5)-disc(8,9,3); c.fill(d,'40c060'); c.fill({(x,y) for x,y in d if x>8 and y<9},'404a58')
    for i,h in enumerate((4,7,5,10,8)): c.fill(rect(18+i*2+i//2*0,12-h,18+i*2,12),'7a5af0')
    pts=[(2,27),(6,24),(9,25),(13,20),(16,22),(19,17),(22,19),(25,18),(29,21)]
    c.fill(polyline(pts),'40b0ff')
    c.fill(rect(19,16,20,17)|rect(29,20,29,22),'f04040'); c.fill(rect(2,29,29,29),'404a58')

@icon('work_alert')
def _(c):
    t=poly([(16,2),(30.5,28),(1.5,28)]); c.fill(t,'f0c020'); c.fill(edge(t),'a07010')
    c.fill(rect(15,10,16,20),'101010'); c.fill(rect(15,23,16,24),'101010')
    for x,y in ((2,4),(29,4),(1,10),(30,10)): c.put(x,y,'f04040')

@icon('work_pull_request')
def _(c):
    G='40c060'; c.fill(rect(0,0,31,31),'14181e')
    c.fill(rect(8,9,9,22),G); c.fill(rect(22,13,23,22),G)
    c.fill(polyline([(23,13),(23,9),(15,9)],2),G); c.fill(poly([(15,5.5),(15,12.5),(11.5,9)]),G)
    for x,y in ((8.5,6),(8.5,25),(22.5,25)):
        c.fill(disc(x,y,3.4),G); c.fill(rect(int(x)-1,int(y)-1,int(x),int(y)),'14181e')

@icon('work_mute')
def _(c):
    c.fill(rrect(10,3,21,19),'d8dce0'); c.fill(rect(11,4,20,18)-rrect(11,4,20,18),'d8dce0')
    for y in (6,9,12,15): c.fill(rect(12,y,19,y),'90949c')
    arc=(disc(15.5,13,9)-disc(15.5,13,7))&rect(0,13,31,31); c.fill(arc,'90949c')
    c.fill(rect(15,22,16,26)|rect(10,27,21,28),'90949c')
    c.fill(line(4,3,28,28,2.6),'e02020')

@icon('work_burndown')
def _(c):
    c.fill(rect(0,0,31,31),'14181e')
    c.fill(rect(3,2,3,28)|rect(3,28,30,28),'a0a0a8')
    c.fill(line(4,4,29,27),'5a6068')
    for x in range(6,30,6): c.fill(rect(x,28,x,29),'a0a0a8')
    c.fill(polyline([(4,4),(8,6),(11,11),(15,12),(18,16),(22,17),(25,22),(29,26)],2),'f08030')
    c.put(29,26,'f8f8f8')

@icon('work_org_chart')
def _(c):
    L='a0a0a8'
    c.fill(rect(15,9,16,12)|rect(5,12,26,13)|rect(5,13,6,17)|rect(15,13,16,17)|rect(25,13,26,17),L)
    for x,y,h,b in ((16,1,'e8b088','e0a020'),(6,18,'c08a60','2a6ad0'),(16,18,'f0c8a0','40a060'),(26,18,'6a4028','d04040')):
        c.fill(disc(x-.5,y+2.5,2.5),h); c.fill(rrect(x-5,y+5,x+4,y+8),b)

@icon('work_chat')
def _(c):
    a=rrect(1,2,22,15); c.fill(a,'7a5af0'); c.fill(poly([(4,15),(9,15),(4,20)]),'7a5af0')
    for y in (5,8,11): c.fill(rect(4,y,4+(15 if y<11 else 9),y+1),'d8d0ff')
    b=rrect(10,16,30,27); c.fill(b,'40a060'); c.fill(poly([(23,27),(28,27),(28,31)]),'40a060')
    for x in (15,20,25): c.fill(rect(x-1,21,x,22),'f0f0f0')
    c.fill(disc(26,3,3),'e02020'); c.fill(rect(26,2,26,4),'f0f0f0')

@icon('work_home_office')
def _(c):
    roof=poly([(16,1),(31,14),(1,14)]); c.fill(roof,'c04030'); c.fill(edge(roof),'802018')
    c.fill(rect(4,14,27,30),'e8d8b8'); c.fill(rect(4,30,27,30),'a08a60')
    c.fill(rect(23,3,25,9),'802018')
    c.fill(rect(7,16,24,27),'5a3418'); c.fill(rect(8,17,23,26),'80c8f8')
    c.fill(rect(11,20,20,24),ALD); c.fill(rect(12,21,19,23),'1a3a8a'); c.fill(rect(9,25,22,26),AL)
    c.fill(rect(21,22,22,24),'f0f0f0')

for name,f in icons.items():
    c=C(); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
