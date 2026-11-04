import math
def hx(s): return (int(s[0:2],16),int(s[2:4],16),int(s[4:6],16),255)
class C:
    def __init__(s,bg='000000'): s.p=[[hx(bg) for _ in range(32)] for _ in range(32)]
    def put(s,x,y,col):
        if 0<=x<32 and 0<=y<32: s.p[y][x]=hx(col)
    def get(s,x,y): return s.p[y][x]
    def fill(s,mask,col):
        for x,y in mask: s.put(x,y,col)
def every(f): return {(x,y) for y in range(32) for x in range(32) if f(x+.5,y+.5)}
def disc(cx,cy,r): return every(lambda x,y:(x-cx)**2+(y-cy)**2<=r*r)
def ell(cx,cy,rx,ry): return every(lambda x,y:((x-cx)/rx)**2+((y-cy)/ry)**2<=1)
def rect(x0,y0,x1,y1): return {(x,y) for x in range(x0,x1+1) for y in range(y0,y1+1) if 0<=x<32 and 0<=y<32}
def poly(pts):
    def inside(x,y):
        n=len(pts); j=n-1; r=False
        for i in range(n):
            xi,yi=pts[i]; xj,yj=pts[j]
            if (yi>y)!=(yj>y) and x<(xj-xi)*(y-yi)/(yj-yi)+xi: r=not r
            j=i
        return r
    return every(inside)
def mirror(m): return m|{(31-x,y) for x,y in m}
def dilate(m,n=1):
    for _ in range(n): m=m|{(x+dx,y+dy) for x,y in m for dx,dy in((1,0),(-1,0),(0,1),(0,-1)) if 0<=x+dx<32 and 0<=y+dy<32}
    return m
def outline(m): return dilate(m)-m
def edge(m): return {(x,y) for x,y in m if any((x+dx,y+dy) not in m for dx,dy in((1,0),(-1,0),(0,1),(0,-1)))}
def line(x0,y0,x1,y1,w=1):
    n=int(max(abs(x1-x0),abs(y1-y0))*2)+1; s=set()
    for i in range(n+1):
        t=i/n; x=x0+(x1-x0)*t; y=y0+(y1-y0)*t
        if w==1: s.add((round(x-.5),round(y-.5)))
        else: s|=disc(x,y,w/2)
    return {(x,y) for x,y in s if 0<=x<32 and 0<=y<32}
def polyline(pts,w=1):
    s=set()
    for a,b in zip(pts,pts[1:]): s|=line(*a,*b,w)
    return s
def art(rows,x0=0,y0=0,mir=False):
    """rows of chars -> {char: mask}; '.' is skipped; mir mirrors a 16-wide left half"""
    out={}
    for r,row in enumerate(rows):
        if mir: row=row+row[::-1]
        for k,ch in enumerate(row):
            if ch!='.' and 0<=x0+k<32 and 0<=y0+r<32: out.setdefault(ch,set()).add((x0+k,y0+r))
    return out
F={'A':".#.|#.#|###|#.#|#.#",'H':"#.#|#.#|###|#.#|#.#",'R':"##.|#.#|##.|#.#|#.#"}
def glyph(ch,x,y,scale=1):
    s=set()
    for r,row in enumerate(F[ch].split('|')):
        for k,v in enumerate(row):
            if v=='#': s|=rect(x+k*scale,y+r*scale,x+k*scale+scale-1,y+r*scale+scale-1)
    return s
