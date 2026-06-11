from PIL import Image, ImageDraw, ImageFont
import math, os

OUT = os.path.join(os.path.dirname(__file__), "katalog.png")
try:
    F = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 20)
    Fs = ImageFont.truetype(r"C:\Windows\Fonts\arial.ttf", 16)
except Exception:
    F = ImageFont.load_default(); Fs = F

# ---------- parametrische Symbole: (d,cx,cy,s,c,bg) ----------
def star(d,cx,cy,s,c,bg):
    pts=[]
    for i in range(10):
        r=s if i%2==0 else s*0.42; a=-math.pi/2+i*math.pi/5
        pts.append((cx+r*math.cos(a),cy+r*math.sin(a)))
    d.polygon(pts,fill=c)
def tree(d,cx,cy,s,c,bg):
    d.rectangle([cx-s*0.1,cy+s*0.55,cx+s*0.1,cy+s*0.85],fill=c)
    for yt,yb,hw in [(-0.9,-0.2,0.45),(-0.45,0.3,0.62),(0.1,0.6,0.8)]:
        d.polygon([(cx,cy+s*yt),(cx-s*hw,cy+s*yb),(cx+s*hw,cy+s*yb)],fill=c)
def snow(d,cx,cy,s,c,bg):
    L=s*0.95; w=max(2,int(s*0.09))
    for k in range(6):
        a=k*math.pi/3; ex,ey=cx+L*math.cos(a),cy+L*math.sin(a)
        d.line([cx,cy,ex,ey],fill=c,width=w)
        for t in (0.55,0.82):
            bx,by=cx+L*t*math.cos(a),cy+L*t*math.sin(a); bl=L*0.24
            for sg in (1,-1):
                aa=a+sg*math.pi/3
                d.line([bx,by,bx+bl*math.cos(aa),by+bl*math.sin(aa)],fill=c,width=w)
def gift(d,cx,cy,s,c,bg):
    d.rectangle([cx-s*0.65,cy-s*0.35,cx+s*0.65,cy+s*0.8],fill=c)
    d.rectangle([cx-s*0.1,cy-s*0.35,cx+s*0.1,cy+s*0.8],fill=bg)
    d.rectangle([cx-s*0.65,cy+s*0.15,cx+s*0.65,cy+s*0.32],fill=bg)
    d.polygon([(cx,cy-s*0.35),(cx-s*0.45,cy-s*0.75),(cx-s*0.05,cy-s*0.5)],fill=c)
    d.polygon([(cx,cy-s*0.35),(cx+s*0.45,cy-s*0.75),(cx+s*0.05,cy-s*0.5)],fill=c)
def bauble(d,cx,cy,s,c,bg):
    d.ellipse([cx-s*0.6,cy-s*0.35,cx+s*0.6,cy+s*0.85],fill=c)
    d.rectangle([cx-s*0.14,cy-s*0.55,cx+s*0.14,cy-s*0.3],fill=c)
    d.arc([cx-s*0.12,cy-s*0.8,cx+s*0.12,cy-s*0.5],180,360,fill=c,width=max(2,int(s*0.08)))
def heart(d,cx,cy,s,c,bg):
    d.ellipse([cx-s*0.62,cy-s*0.5,cx-s*0.02,cy+s*0.1],fill=c)
    d.ellipse([cx+s*0.02,cy-s*0.5,cx+s*0.62,cy+s*0.1],fill=c)
    d.polygon([(cx-s*0.55,cy-s*0.1),(cx+s*0.55,cy-s*0.1),(cx,cy+s*0.7)],fill=c)
def bell(d,cx,cy,s,c,bg):
    d.polygon([(cx,cy-s*0.7),(cx-s*0.55,cy+s*0.35),(cx+s*0.55,cy+s*0.35)],fill=c)
    d.ellipse([cx-s*0.6,cy+s*0.2,cx+s*0.6,cy+s*0.5],fill=c)
    d.ellipse([cx-s*0.12,cy+s*0.45,cx+s*0.12,cy+s*0.7],fill=c)
def candle(d,cx,cy,s,c,bg):
    d.rectangle([cx-s*0.22,cy-s*0.3,cx+s*0.22,cy+s*0.8],fill=c)
    d.ellipse([cx-s*0.18,cy-s*0.75,cx+s*0.18,cy-s*0.3],fill=c)  # Flamme
    d.line([cx,cy-s*0.3,cx,cy-s*0.45],fill=bg,width=max(2,int(s*0.06)))
def moon(d,cx,cy,s,c,bg):
    d.ellipse([cx-s*0.7,cy-s*0.7,cx+s*0.7,cy+s*0.7],fill=c)
    d.ellipse([cx-s*0.35,cy-s*0.7,cx+s*1.05,cy+s*0.7],fill=bg)
def diamond(d,cx,cy,s,c,bg):
    d.polygon([(cx,cy-s*0.85),(cx+s*0.6,cy),(cx,cy+s*0.85),(cx-s*0.6,cy)],fill=c)
def cross(d,cx,cy,s,c,bg):
    d.rectangle([cx-s*0.18,cy-s*0.7,cx+s*0.18,cy+s*0.7],fill=c)
    d.rectangle([cx-s*0.7,cy-s*0.18,cx+s*0.7,cy+s*0.18],fill=c)
def disc(d,cx,cy,s,c,bg):
    d.ellipse([cx-s*0.7,cy-s*0.7,cx+s*0.7,cy+s*0.7],fill=c)
def snowman(d,cx,cy,s,c,bg):
    d.ellipse([cx-s*0.55,cy+s*0.05,cx+s*0.55,cy+s*0.9],fill=c)
    d.ellipse([cx-s*0.4,cy-s*0.4,cx+s*0.4,cy+s*0.3],fill=c)
    d.ellipse([cx-s*0.28,cy-s*0.85,cx+s*0.28,cy-s*0.3],fill=c)
def bow(d,cx,cy,s,c,bg):
    d.polygon([(cx,cy),(cx-s*0.7,cy-s*0.45),(cx-s*0.7,cy+s*0.45)],fill=c)
    d.polygon([(cx,cy),(cx+s*0.7,cy-s*0.45),(cx+s*0.7,cy+s*0.45)],fill=c)
    d.ellipse([cx-s*0.18,cy-s*0.18,cx+s*0.18,cy+s*0.18],fill=c)
def candy(d,cx,cy,s,c,bg):
    w=int(s*0.42)
    pts=[(cx-s*0.25,cy+s*0.85),(cx-s*0.25,cy-s*0.3)]
    for ang in range(180,-1,-20):
        a=math.radians(ang); pts.append((cx-s*0.25+s*0.35*(1-math.cos(a))*0+ s*0.35+ s*0.35*math.cos(a), cy-s*0.3 - s*0.35*math.sin(a)))
    d.line(pts,fill=c,width=w,joint="curve")
    # Streifen
    for off in range(-1,9):
        yy=cy-s*0.2+off*s*0.22
        d.line([cx-s*0.55,yy- s*0.12,cx+s*0.05,yy+ s*0.12],fill=bg,width=max(2,int(s*0.07)))
def key(d,cx,cy,s,c,bg):
    d.ellipse([cx-s*0.7,cy-s*0.45,cx-s*0.05,cy+s*0.2],fill=c)
    d.ellipse([cx-s*0.5,cy-s*0.25,cx-s*0.25,cy],fill=bg)
    d.rectangle([cx-s*0.1,cy-s*0.12,cx+s*0.75,cy+s*0.12],fill=c)
    d.rectangle([cx+s*0.5,cy+s*0.1,cx+s*0.6,cy+s*0.4],fill=c)
    d.rectangle([cx+s*0.66,cy+s*0.1,cx+s*0.76,cy+s*0.45],fill=c)

SYMS = [
 ("Stern",star),("Tannenbaum",tree),("Schneeflocke",snow),("Geschenk",gift),
 ("Christbaumkugel",bauble),("Herz",heart),("Glocke",bell),("Kerze",candle),
 ("Mond",moon),("Raute",diamond),("Kreuz",cross),("Punkt/Kreis",disc),
 ("Schneemann",snowman),("Schleife",bow),("Zuckerstange",candy),("Schluessel",key),
]

COLORS = [
 ("Tannenrot","9E2B25"),("Tannengruen","2E6B3E"),("Mitternachtsblau","2A4A7A"),("Gold","C79A3A"),
 ("Burgund","6E2438"),("Petrol","2A7D7D"),("Creme","E8DDC7"),("Schokobraun","6B4A2E"),
 ("Pflaume","5A3A6E"),("Kuerbisorange","C4622D"),("Eisblau","6FA8C7"),("Salbeigruen","7C9A6E"),
 ("Beere","B23A6E"),("Anthrazit","3A3A42"),("Karamell","B5793A"),("Smaragd","1F8A5B"),
]

def hx(h): return tuple(int(h[i:i+2],16) for i in (0,2,4))

# ---------- Layout ----------
PAD=24; CELL=150; GAP=16; COLS=4
def grid_h(n): return ((n+COLS-1)//COLS)*(CELL+34)
Wd = PAD*2 + COLS*CELL + (COLS-1)*GAP
H1 = 50 + grid_h(len(COLORS))
H2 = 50 + grid_h(len(SYMS))
Ht = PAD*2 + H1 + 30 + H2
img = Image.new("RGB",(Wd,Ht),(245,245,245))
d = ImageDraw.Draw(img)

def cell_xy(i,y0):
    r=i//COLS; c=i%COLS
    return PAD+c*(CELL+GAP), y0+r*(CELL+34)

SS=4
def render_symbol(fn,base,col):
    t=Image.new("RGB",(CELL*SS,CELL*SS),base); dd=ImageDraw.Draw(t)
    fn(dd,CELL*SS/2,CELL*SS/2,CELL*SS*0.34,col,base)
    return t.resize((CELL,CELL),Image.LANCZOS)

y=PAD
d.text((PAD,y),"FARBEN (waehle 8)",font=F,fill=(20,20,20)); y+=40
for i,(nm,h) in enumerate(COLORS):
    x,yy=cell_xy(i,y); d.rectangle([x,yy,x+CELL,yy+CELL],fill=hx(h))
    d.text((x,yy+CELL+4),f"{nm}",font=Fs,fill=(20,20,20))
    d.text((x,yy+CELL+18),f"#{h}",font=Fs,fill=(90,90,90))
y += grid_h(len(COLORS))+30
d.text((PAD,y),"SYMBOLE (waehle 10)",font=F,fill=(20,20,20)); y+=40
tile_bg=hx("2A4A7A"); sym_col=hx("F4ECD8")
for i,(nm,fn) in enumerate(SYMS):
    x,yy=cell_xy(i,y)
    img.paste(render_symbol(fn,tile_bg,sym_col),(x,yy))
    d.text((x,yy+CELL+6),nm,font=Fs,fill=(20,20,20))

img.save(OUT); print("Gespeichert:",OUT,img.size)
