import zlib, struct
def read(path):
    d=open(path,'rb').read(); p=8; idat=b''; plte=None; trns=None
    while p<len(d):
        n,t=struct.unpack('>I4s',d[p:p+8]); c=d[p+8:p+8+n]; p+=12+n
        if t==b'IHDR': w,h,bd,ct=struct.unpack('>IIBB',c[:10])
        elif t==b'PLTE': plte=[tuple(c[i:i+3]) for i in range(0,n,3)]
        elif t==b'tRNS': trns=c
        elif t==b'IDAT': idat+=c
    assert bd==8,(bd,ct)
    bpp={2:3,6:4,3:1,0:1,4:2}[ct]; raw=zlib.decompress(idat); stride=w*bpp; rows=[]; prev=bytearray(stride); i=0
    for y in range(h):
        f=raw[i]; line=bytearray(raw[i+1:i+1+stride]); i+=1+stride
        for x in range(stride):
            a=line[x-bpp] if x>=bpp else 0; b=prev[x]; c2=prev[x-bpp] if x>=bpp else 0
            if f==1: line[x]=(line[x]+a)&255
            elif f==2: line[x]=(line[x]+b)&255
            elif f==3: line[x]=(line[x]+(a+b)//2)&255
            elif f==4:
                pa,pb,pc=abs(b-c2),abs(a-c2),abs(a+b-2*c2)
                pr=a if pa<=pb and pa<=pc else (b if pb<=pc else c2); line[x]=(line[x]+pr)&255
        rows.append(line); prev=line
    px=[]
    for line in rows:
        r=[]
        for x in range(w):
            v=line[x*bpp:(x+1)*bpp]
            if ct==2: r.append((v[0],v[1],v[2],255))
            elif ct==6: r.append(tuple(v))
            elif ct==3: col=plte[v[0]]; r.append(col+((trns[v[0]] if trns and v[0]<len(trns) else 255),))
            elif ct==0: r.append((v[0],)*3+(255,))
            elif ct==4: r.append((v[0],)*3+(v[1],))
        px.append(r)
    return w,h,ct,px
def write(path,px):
    h=len(px); w=len(px[0]); raw=b''.join(b'\0'+bytes(c for p in row for c in p) for row in px)
    def ch(t,c): return struct.pack('>I',len(c))+t+c+struct.pack('>I',zlib.crc32(t+c))
    open(path,'wb').write(b'\x89PNG\r\n\x1a\n'+ch(b'IHDR',struct.pack('>IIBBBBB',w,h,8,6,0,0,0))+ch(b'IDAT',zlib.compress(raw,9))+ch(b'IEND',b''))
