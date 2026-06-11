from PIL import Image, ImageDraw, ImageFont
import math, os
OUT = os.path.join(os.path.dirname(__file__), "symbole_mehr.png")
try:
    Fb=ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf",18); Fs=ImageFont.truetype(r"C:\Windows\Fonts\arial.ttf",14)
except Exception:
    Fb=ImageFont.load_default(); Fs=Fb

# --- bereits vorhanden (ungenutzt) ---
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
def bow(d,cx,cy,s,c,bg):
    d.polygon([(cx,cy),(cx-s*0.7,cy-s*0.45),(cx-s*0.7,cy+s*0.45)],fill=c)
    d.polygon([(cx,cy),(cx+s*0.7,cy-s*0.45),(cx+s*0.7,cy+s*0.45)],fill=c)
    d.ellipse([cx-s*0.18,cy-s*0.18,cx+s*0.18,cy+s*0.18],fill=c)
def candy(d,cx,cy,s,c,bg):
    w=int(s*0.42); pts=[(cx-s*0.25,cy+s*0.85),(cx-s*0.25,cy-s*0.3)]
    for ang in range(180,-1,-20):
        a=math.radians(ang); pts.append((cx+s*0.35+s*0.35*math.cos(a), cy-s*0.3-s*0.35*math.sin(a)))
    d.line(pts,fill=c,width=w,joint="curve")
    for off in range(-1,9):
        yy=cy-s*0.2+off*s*0.22
        d.line([cx-s*0.55,yy-s*0.12,cx+s*0.05,yy+s*0.12],fill=bg,width=max(2,int(s*0.07)))

# --- neu ---
def crown(d,cx,cy,s,c,bg):
    d.polygon([(cx-0.7*s,0.45*s+cy),(cx-0.7*s,0.0+cy),(cx-0.45*s,0.1*s+cy),(cx-0.22*s,-0.5*s+cy),
               (cx,0.05*s+cy),(cx+0.22*s,-0.5*s+cy),(cx+0.45*s,0.1*s+cy),(cx+0.7*s,0.0+cy),(cx+0.7*s,0.45*s+cy)],fill=c)
def leaf(d,cx,cy,s,c,bg):
    d.polygon([(cx,cy-0.7*s),(cx+0.38*s,cy),(cx,cy+0.7*s),(cx-0.38*s,cy)],fill=c)
    d.line([cx,cy-0.55*s,cx,cy+0.7*s],fill=bg,width=max(2,int(s*0.06)))
def santahat(d,cx,cy,s,c,bg):
    d.polygon([(cx-0.55*s,cy+0.2*s),(cx+0.5*s,cy+0.05*s),(cx+0.15*s,cy-0.6*s)],fill=c)
    d.rounded_rectangle([cx-0.6*s,cy+0.18*s,cx+0.55*s,cy+0.42*s],radius=int(s*0.12),fill=c)
    d.ellipse([cx+0.02*s,cy-0.72*s,cx+0.3*s,cy-0.44*s],fill=c)
def stocking(d,cx,cy,s,c,bg):
    d.polygon([(cx-0.22*s,cy-0.6*s),(cx+0.2*s,cy-0.6*s),(cx+0.2*s,cy+0.12*s),
               (cx+0.55*s,cy+0.34*s),(cx+0.55*s,cy+0.6*s),(cx-0.05*s,cy+0.46*s),(cx-0.22*s,cy+0.28*s)],fill=c)
    d.rectangle([cx-0.24*s,cy-0.62*s,cx+0.22*s,cy-0.46*s],fill=c)
def mug(d,cx,cy,s,c,bg):
    d.ellipse([cx+0.1*s,cy-0.28*s,cx+0.62*s,cy+0.28*s],fill=c)
    d.ellipse([cx+0.22*s,cy-0.16*s,cx+0.5*s,cy+0.16*s],fill=bg)
    d.rounded_rectangle([cx-0.5*s,cy-0.4*s,cx+0.28*s,cy+0.5*s],radius=int(s*0.1),fill=c)
def gingerbread(d,cx,cy,s,c,bg):
    d.ellipse([cx-0.22*s,cy-0.72*s,cx+0.22*s,cy-0.28*s],fill=c)         # Kopf
    d.rounded_rectangle([cx-0.26*s,cy-0.32*s,cx+0.26*s,cy+0.35*s],radius=int(s*0.18),fill=c) # Rumpf
    d.rounded_rectangle([cx-0.62*s,cy-0.28*s,cx-0.18*s,cy-0.02*s],radius=int(s*0.1),fill=c)  # Arm L
    d.rounded_rectangle([cx+0.18*s,cy-0.28*s,cx+0.62*s,cy-0.02*s],radius=int(s*0.1),fill=c)  # Arm R
    d.rounded_rectangle([cx-0.26*s,cy+0.2*s,cx-0.02*s,cy+0.68*s],radius=int(s*0.1),fill=c)   # Bein L
    d.rounded_rectangle([cx+0.02*s,cy+0.2*s,cx+0.26*s,cy+0.68*s],radius=int(s*0.1),fill=c)   # Bein R

CANDS=[("Mond",moon),("Raute",diamond),("Kreuz",cross),("Punkt/Kreis",disc),
       ("Schleife",bow),("Zuckerstange",candy),("Krone",crown),("Blatt",leaf),
       ("Weihnachtsmuetze",santahat),("Socke",stocking),("Tasse",mug),("Lebkuchenmann",gingerbread)]

CELL=150; COLS=4; PAD=20; SS=4
rows=(len(CANDS)+COLS-1)//COLS
W=PAD*2+COLS*CELL; H=PAD+40+rows*(CELL+30)
img=Image.new("RGB",(W,H),(245,245,245)); d=ImageDraw.Draw(img)
d.text((PAD,PAD),"ZUSAETZLICHE SYMBOLE (waehle 2)",font=Fb,fill=(20,20,20))
bg=(42,74,122); col=(255,255,255)
for i,(nm,fn) in enumerate(CANDS):
    r=i//COLS; c=i%COLS; x=PAD+c*CELL; y=PAD+40+r*(CELL+30)
    t=Image.new("RGB",(CELL*SS,CELL*SS),bg); dd=ImageDraw.Draw(t)
    fn(dd,CELL*SS/2,CELL*SS/2,CELL*SS*0.32,col,bg)
    img.paste(t.resize((CELL-8,CELL-8),Image.LANCZOS),(x,y))
    d.text((x,y+CELL-2),nm,font=Fs,fill=(20,20,20))
img.save(OUT); print("Gespeichert:",OUT,img.size)
