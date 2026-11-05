/* Version-scoped research runtime. Never installed by DllMain.
 * Qt testability is a controlled launch mechanism, not an official WeChat API.
 * All object mutation is confined to the GUI callback thread. */
#define WIN32_LEAN_AND_MEAN
#define _WIN32_WINNT 0x0601
#include <windows.h>
typedef LRESULT (CALLBACK *ThemeSubclassProc)(HWND,UINT,WPARAM,LPARAM,UINT_PTR,DWORD_PTR);
typedef BOOL (WINAPI *ThemeSetSubclass)(HWND,ThemeSubclassProc,UINT_PTR,DWORD_PTR);
typedef BOOL (WINAPI *ThemeRemoveSubclass)(HWND,ThemeSubclassProc,UINT_PTR);
typedef LRESULT (WINAPI *ThemeDefaultSubclass)(HWND,UINT,WPARAM,LPARAM);
static ThemeSetSubclass setSubclass;
static ThemeRemoveSubclass removeSubclass;
static ThemeDefaultSubclass defaultSubclass;
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <wchar.h>
#include <stdlib.h>

#define MAX_OBJECTS 4096
#define EXPORT __declspec(dllexport)
typedef unsigned char byte;
typedef uintptr_t up;
typedef struct { unsigned spec; unsigned short a,r,g,b,pad[2]; up tail; } QColor;
typedef struct { int ref,len,alloc,pad; intptr_t offset; wchar_t text[1024]; } QStringData;
typedef struct { QStringData *d; } QString;
typedef struct { unsigned text,chat,secondary,canvas,link,icons,accent,selectedText; int iconMode,brightness,saturation,contrast,visibility,inputFull; void *wall,*inputBorder,*masks[5],*frost[3]; } Profile;
typedef struct { void *obj,*vtable,*delegate,*identity,*widgetData; byte clone[264]; unsigned ink,appliedInk; void *paragraph; unsigned link,appliedLink; byte flag; int fill,kind; unsigned iconGeneration,textGeneration,paletteGeneration,rowSelected; } Record;
typedef struct { unsigned size,version,enabled,active,records,paints,scans,errors,generation; } Status;
static byte *image;
static Profile profiles[2];
static Record records[MAX_OBJECTS];
static Status status={sizeof(Status),1};
static void *mainRoot;
static int busy,installed,paletteOwned,paletteMode,dirty=1;
static unsigned uiReadyGeneration;
static int observedMode=-1;
static int explicitGuiRefresh;
static unsigned paletteNotifications;
static LONG applyPending;
static byte oldPalette[8][16],oldDual[8][8];
static const int indices[8]={1,2,3,4,18,6,248,249};
static unsigned palette[16];
static wchar_t configPath[1024],baseConfigPath[1024],previewControl[1024];
static UINT_PTR configTimer;
static HWND controlWindow;
static void attachControl(void);
static DWORD lastCheck,guiThread,lastScanMs,maxScanMs;
static void logStatus(const char *event){wchar_t path[1100];FILE *f;size_t n=wcslen(configPath);if(!n)return;wcscpy(path,configPath);while(n&&path[n-1]!=L'\\')n--;path[n]=0;wcscat(path,L"native-runtime.log");f=_wfopen(path,L"a");if(f){fprintf(f,"tick=%lu event=%s enabled=%u active=%u records=%u paints=%u scans=%u errors=%u generation=%u scanMs=%lu maxScanMs=%lu\n",GetTickCount(),event,status.enabled,status.active,status.records,status.paints,status.scans,status.errors,status.generation,lastScanMs,maxScanMs);fclose(f);}}
static FILETIME lastWrite;
static HMODULE own;
typedef intptr_t (*EventFn)(void*,void*);
typedef intptr_t (*PaintFn)(void*,void*);
typedef QColor *(*IconFn)(void*,QColor*);
typedef void *(*RowFn)(void*,void*);
typedef void *(*SvgFn)(void*,QString*,void*,void*);
static EventFn oldEvent;
static PaintFn oldText;
static IconFn oldIcon;
static RowFn oldRow;
static SvgFn oldSvg;
typedef void *(*DataFn)(void*,QString*);
static DataFn oldData;
static int mode(void);
static int readable(const void*,size_t);
typedef struct { QStringData uri[2],file[2]; QString qfile[2]; unsigned preserveColor[2]; } Route;
static Route routes[4096];
static int routeCount;static unsigned svgPlateHits,svgDecoded,svgFallbacks;
static void *svgGet(void *a,QString *path,void *c,void *d){int i,m=mode();QStringData *s=path?path->d:NULL;if((!guiThread||GetCurrentThreadId()==guiThread)&&status.enabled&&mainRoot&&profiles[m].iconMode!=0&&readable(s,24)&&s->len>0&&s->len<512&&s->offset>=0&&s->offset<65536&&readable((byte*)s+s->offset,s->len*2)){for(i=0;i<routeCount;i++){QStringData *u=&routes[i].uri[m];if(s->len==u->len&&!memcmp((byte*)s+s->offset,u->text,s->len*2)){void *result;if(routes[i].preserveColor[m]&&readable(c,16)){unsigned options[4];memcpy(options,c,16);options[2]=0;if(++svgPlateHits==1)logStatus("button-literal-colors-first-hit");result=oldSvg(a,&routes[i].qfile[m],options,d);}else result=oldSvg(a,&routes[i].qfile[m],c,d);if(readable(a,8)&&!*(void**)a){if(++svgFallbacks==1)logStatus("svg-replacement-failed-original-fallback");return oldSvg(a,path,c,d);}if(++svgDecoded==1)logStatus("svg-replacement-decoded-first-hit");return result;}}}return oldSvg(a,path,c,d);}
/* WeChat's 06f860 rejects paths without a ':' prefix. Supplying a local
 * filename to that resource-only reader cannot load replacement SVGs.
 * Intercept ONLY prepared route QString data, retaining the original reader
 * for every other request. SkData owns the buffer through its release proc. */
static void releaseSvgBytes(const void *bytes,void *context){free(context);}
static void *assetData(void *out,QString *path){int i,m;QStringData *s=path?path->d:NULL;for(i=0;status.enabled&&mainRoot&&i<routeCount;i++)for(m=0;m<2;m++)if(s==&routes[i].file[m]){FILE *f;long size;byte *bytes;*(void**)out=NULL;f=_wfopen(s->text,L"rb");if(!f)return out;if(fseek(f,0,SEEK_END)||(size=ftell(f))<1||size>1048576||fseek(f,0,SEEK_SET)){fclose(f);return out;}bytes=malloc(size);if(!bytes){fclose(f);return out;}if(fread(bytes,1,size,f)!=(size_t)size){free(bytes);fclose(f);return out;}fclose(f);((void*(*)(void*,const void*,size_t,void*,void*))(image+0x740b0))(out,bytes,(size_t)size,releaseSvgBytes,bytes);if(!*(void**)out)free(bytes);return out;}return oldData(out,path);}

static byte callOriginal[6][5];
static void *callStub[6];
static int readable(const void *p,size_t n){ MEMORY_BASIC_INFORMATION m; up a=(up)p; if(!a||n>0x1000000||a+n<a)return 0; if(!VirtualQuery(p,&m,sizeof m)||m.State!=MEM_COMMIT||(m.Protect&(PAGE_GUARD|PAGE_NOACCESS)))return 0; return a+n<=(up)m.BaseAddress+m.RegionSize; }
static void *ptr(const void *p){return readable(p,8)?*(void *const*)p:NULL;}
static int patch(void *p,const void *v,size_t n){DWORD old; if(!VirtualProtect(p,n,PAGE_READWRITE,&old))return 0; memcpy(p,v,n); VirtualProtect(p,n,old,&old); FlushInstructionCache(GetCurrentProcess(),p,n); return 1;}
static void *parent(void *o){return ptr((byte*)ptr((byte*)o+8)+0x10);}
static int mode(void){return image&&image[0xbbd93c6]?1:0;}
static int observeMode(void){int current=mode();if(current==observedMode)return 0;observedMode=current;status.generation++;dirty=1;return 1;}
static const char *className(void *o);
static void *wallRoot;
static int scope(void *o,int *chat){ int i; *chat=0; for(i=0;o&&i<48;i++,o=parent(o)){void *v=ptr(o); if(v==image+0x934fbd8)*chat=1;else if(!*chat&&v==image+0x93587c8)*chat=2; if(v==image+0x983c628)return 1;} return 0; }
typedef struct {void *table;const char *name;} ClassCache;
static ClassCache classCache[2048];
static const char *className(void *o){
 void *table=ptr(o);byte *fn;const char *found="?";int i,j,slot=-1;
 if((byte*)table>=image&&(byte*)table<image+0xc000000){unsigned hash=((up)table>>4)&2047;for(j=0;j<16;j++){unsigned k=(hash+j)&2047;if(classCache[k].table==table)return classCache[k].name;if(!classCache[k].table){slot=k;break;}}}
 fn=ptr(table);if(readable(fn,128))for(i=0;i<121;i++)if(fn[i]==0x48&&fn[i+1]==0x8d&&(fn[i+2]==5||fn[i+2]==13)){
  byte *meta=fn+i+7+*(int*)(fn+i+3),*str;int n,valid=1;intptr_t offset;
  if(meta<image||meta>image+0xc000000)continue;str=ptr(meta+8);if(!readable(str,24))continue;n=*(int*)(str+4);offset=*(intptr_t*)(str+16);
  if(n<1||n>=160||offset<0||offset>=65536||!readable(str+offset,n+1)||str[offset+n])continue;
  for(j=0;j<n;j++){byte c=str[offset+j];if(!((c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c==':'||c=='_')){valid=0;break;}}
  if(valid){found=(const char*)str+offset;break;}
 }
 if(slot>=0){classCache[slot].table=table;classCache[slot].name=found;}return found;
}
static int excluded(const char *s){return strstr(s,"Avatar")||strstr(s,"HeadView")||strstr(s,"Emoji")||strstr(s,"Emoticon");}
static int protectedIcon(void *o){int i;for(i=0;o&&i<48;i++,o=parent(o)){const char *s=className(o);if(excluded(s)||!strcmp(s,"mmui::MessageView"))return 1;if(ptr(o)==image+0x983c628)return 0;}return 1;}
#define RECORD_SLOTS 8192
static unsigned short recordIndex[RECORD_SLOTS];
static unsigned objectHash(void *o,unsigned mask){
    up address=(up)o;
    return (unsigned)((address>>4)^(address>>17))&mask;
}
static unsigned recordSlot(void *o){
    unsigned slot=objectHash(o,RECORD_SLOTS-1);
    while(recordIndex[slot]&&records[recordIndex[slot]-1].obj!=o)
        slot=(slot+1)&(RECORD_SLOTS-1);
    return slot;
}
static Record *record(void *o){
    unsigned slot=recordSlot(o);
    void *v=ptr(o),*id=ptr((byte*)o+8),*data=ptr((byte*)o+0x28);
    Record *r=recordIndex[slot]?&records[recordIndex[slot]-1]:NULL;
    if(r&&r->vtable==v&&r->identity==id&&r->widgetData==data)return r;
    if(status.records==MAX_OBJECTS){status.errors++;return NULL;}
    r=&records[status.records++];
    recordIndex[slot]=(unsigned short)status.records;
    memset(r,0,sizeof *r);
    r->obj=o;r->vtable=v;r->identity=id;r->widgetData=data;r->fill=-1;
    return r;
}
static int live(Record *r){return readable(r->obj,0x168)&&ptr(r->obj)==r->vtable&&ptr((byte*)r->obj+8)==r->identity&&ptr((byte*)r->obj+0x28)==r->widgetData;}
static void qcolor(QColor *q,unsigned c){q->spec=1;q->a=65535;q->r=((c>>16)&255)*257;q->g=((c>>8)&255)*257;q->b=(c&255)*257;q->pad[0]=q->pad[1]=0;}
static void paletteApply(void){int i,m=mode();if(!status.enabled||!mainRoot)return;if(!paletteOwned){paletteMode=mode();for(i=0;i<8;i++){memcpy(oldPalette[i],image+0xbba9d30+indices[i]*24,16);memcpy(oldDual[i],image+0xb897a60+indices[i]*8,8);}paletteOwned=1;}for(i=0;i<8;i++){int k;QColor q;for(k=0;k<2;k++){unsigned c=palette[i*2+k];byte *d=image+0xb897a60+indices[i]*8+k*4;byte value[4]={(byte)(c>>16),(byte)(c>>8),(byte)c,255};if(memcmp(d,value,4))patch(d,value,4);}qcolor(&q,palette[i*2+m]);if(memcmp(image+0xbba9d30+indices[i]*24,&q,16))memcpy(image+0xbba9d30+indices[i]*24,&q,16);}}
static int geometry(void *o,float *w,float *h){byte *d=ptr((byte*)o+0x28);if(!readable(d,0x24))return 0;*w=*(int*)(d+0x1c)-*(int*)(d+0x14)+1;*h=*(int*)(d+0x20)-*(int*)(d+0x18)+1;return *w>0&&*h>0&&*w<=16384&&*h<=16384;}
static void drawCover(void *canvas,void *sk,float w,float h){float iw,ih,s,rect[4];up opts[3]={0,0,1};if(!readable(sk,0x28))return;iw=*(int*)((byte*)sk+0x20);ih=*(int*)((byte*)sk+0x24);if(iw<=0||ih<=0)return;s=w/iw>h/ih?w/iw:h/ih;rect[0]=(w-iw*s)*.5f;rect[1]=(h-ih*s)*.5f;rect[2]=rect[0]+iw*s;rect[3]=rect[1]+ih*s;((void(*)(void*,void*,float*,void*,void*))(image+0x53fa30))(canvas,sk,rect,opts,NULL);}
static void drawMaterial(void *canvas,void *sk,float w,float h){float rect[4]={0,0,w,h};up opts[3]={0,0,1};if(!readable(sk,0x28)||w<=0||h<=0)return;((void(*)(void*,void*,float*,void*,void*))(image+0x53fa30))(canvas,sk,rect,opts,NULL);}
static void drawInputBorder(void *canvas,void *sk,float w,float h){int r,c;float a[]={0,24,40,64},x[4]={0,24,w-24,w},y[4]={0,24,h-64,h-40};up opts[3]={0,0,1};typedef void(*Nine)(void*,void*,float*,float*,void*,void*,int);Nine fn=(Nine)ptr((byte*)ptr(canvas)+0x118);if(!sk||!fn||w<80||h<108)return;for(r=0;r<3;r++)for(c=0;c<3;c++){float src[4]={a[c],a[r],a[c+1],a[r+1]},dst[4]={x[c],y[r],x[c+1],y[r+1]};fn(canvas,sk,src,dst,opts,NULL,1);}}
static void drawInputFull(void *canvas,void *sk,float w,float h){int r,c;float a[]={0,24,40,64},x[4]={0,24,w-24,w},y[4]={0,24,h-28,h-4};up opts[3]={0,0,1};typedef void(*Nine)(void*,void*,float*,float*,void*,void*,int);Nine fn=(Nine)ptr((byte*)ptr(canvas)+0x118);if(!sk||!fn||w<80||h<56)return;for(r=0;r<3;r++)for(c=0;c<3;c++){float src[4]={a[c],a[r],a[c+1],a[r+1]},dst[4]={x[c],y[r],x[c+1],y[r+1]};fn(canvas,sk,src,dst,opts,NULL,1);}}
static int rootPosition(void *o,float *x,float *y){int i;*x=*y=0;for(i=0;o&&o!=mainRoot&&i<48;i++,o=parent(o)){byte *d=ptr((byte*)o+0x28);if(!readable(d,0x24))return 0;*x+=*(int*)(d+0x14);*y+=*(int*)(d+0x18);}return o==mainRoot;}
static void drawRegionFrost(void *canvas,void *sk,void *o){float w,h,x,y,bx,by,iw,ih,scale,rect[4];up opts[3]={0,0,1};if(!sk||!wallRoot||!geometry(wallRoot,&w,&h)||!rootPosition(o,&x,&y)||!rootPosition(wallRoot,&bx,&by)||!readable(sk,0x28))return;iw=*(int*)((byte*)sk+0x20);ih=*(int*)((byte*)sk+0x24);if(iw<=0||ih<=0)return;scale=w/iw>h/ih?w/iw:h/ih;rect[0]=(w-iw*scale)/2-(x-bx);rect[1]=(h-ih*scale)/2-(y-by);rect[2]=rect[0]+iw*scale;rect[3]=rect[1]+ih*scale;((void(*)(void*,void*,float*,void*,void*))(image+0x53fa30))(canvas,sk,rect,opts,NULL);}
/* Continue the session material above the list, behind its search controls. */
static void drawSessionHeader(void *canvas,void *root,float w,float h,Profile *profile){
    unsigned i;
    float region[4]={0,0,0,0}, widest=0,bx,by;
    void *sk=profile->frost[0];
    if((!profile->masks[0]&&!sk)||!rootPosition(root,&bx,&by))return;
    /* Startup can retain a narrow session list while the full list is mounted. */
    for(i=0;i<status.records;i++){
        Record *r=&records[i];
        float x,y,listWidth,listHeight;
        if(r->kind!=3||!live(r)||(r->vtable==image+0x9d1e568&&(!readable(r->widgetData,12)||!(*(unsigned*)((byte*)r->widgetData+8)&32768)))||ptr((byte*)r->obj+0xc0)!=r->clone+8)continue;
        if(!geometry(r->obj,&listWidth,&listHeight)||!rootPosition(r->obj,&x,&y))continue;
        x-=bx;
        y-=by;
        if(x<0||y<=0||y>h/3||x+listWidth>w||listWidth<=widest)continue;
        widest=listWidth;
        region[0]=x;region[2]=x+listWidth;region[3]=y;
    }
    if(!widest)return;
    if(readable(sk,0x28)){
        float iw=*(int*)((byte*)sk+0x20),ih=*(int*)((byte*)sk+0x24);
        typedef void(*DrawRect)(void*,void*,float*,float*,void*,void*,int);
        DrawRect draw=(DrawRect)ptr((byte*)ptr(canvas)+0x118);
        if(draw&&iw>0&&ih>0){
            float scale=w/iw>h/ih?w/iw:h/ih;
            float left=(w-iw*scale)*.5f,top=(h-ih*scale)*.5f;
            float src[4]={(region[0]-left)/scale,-top/scale,(region[2]-left)/scale,(region[3]-top)/scale};
            up opts[3]={0,0,1};
            draw(canvas,sk,src,region,opts,NULL,1);
        }
    }
    if(readable(profile->masks[0],0x28)){
        up opts[3]={0,0,1};
        ((void(*)(void*,void*,float*,void*,void*))(image+0x53fa30))(canvas,profile->masks[0],region,opts,NULL);
    }
}
static int inContacts(void *o);
static intptr_t surfacePaint(void *self,void *canvas){void *o=(byte*)self-0xc0;unsigned i;float w,h;Profile *p=&profiles[mode()];if(!status.enabled||!geometry(o,&w,&h))return 0;i=recordIndex[recordSlot(o)];if(i&&ptr((byte*)o+0xc0)==records[--i].clone+8){int k=records[i].kind;status.paints++;if(!p->wall&&inContacts(o)){PaintFn fn=(PaintFn)ptr((byte*)records[i].delegate+8);return fn?fn(self,canvas):0;}if(k==1){wallRoot=o;if(p->wall){drawCover(canvas,p->wall,w,h);drawSessionHeader(canvas,o,w,h,p);}else{PaintFn fn=(PaintFn)ptr((byte*)records[i].delegate+8);if(fn)return fn(self,canvas);}}else if(k==2){drawRegionFrost(canvas,p->frost[2],o);if(p->inputFull)drawInputFull(canvas,p->inputBorder,w,h);else drawInputBorder(canvas,p->inputBorder,w,h);}else if(k==8){drawRegionFrost(canvas,p->frost[0],o);drawMaterial(canvas,p->masks[0],w,h);}else if(k>=3&&k<8){if(k==3)drawRegionFrost(canvas,p->frost[0],o);else if(k==7)drawRegionFrost(canvas,p->frost[1],o);else if(k==5)drawRegionFrost(canvas,p->frost[2],o);drawMaterial(canvas,p->masks[k-3],w,h);}}return 0;}
/* These vtables identify the main-window contacts page in the verified x64 build. */
static int inContacts(void *o){
    int depth;
    for(depth=0;o&&depth<24;depth++,o=parent(o)){
        if(ptr(o)==image+0x9b5da48||ptr(o)==image+0x9b5e3f8)return 1;
        if(o==mainRoot)break;
    }
    return 0;
}
static int contactSurface(void *o,const char *name){
    void *vt=ptr(o);
    if(vt!=image+0x9d1e568&&vt!=image+0x9b5e3f8&&vt!=image+0x9d4c798&&vt!=image+0x93add78)return -1;
    if(!profiles[mode()].wall||!inContacts(o))return -1;
    if(vt==image+0x9d1e568)return 3;
    if(vt==image+0x9b5e3f8)return 0;
    if(vt==image+0x9d4c798)return 8;
    if(!strcmp(name,"mmui::XVBoxView")&&ptr(parent(o))==image+0x9b5e3f8)return 0;
    return -1;
}
static int allowed(const char *s){const char *a[]={"mmui::MainView","mmui::MainTabBar","mmui::ChatSessionList","mmui::ChatMasterView","mmui::ChatDetailView","mmui::ChatMessagePage","mmui::ChatInputView","mmui::ChatEditView","mmui::XView","mmui::XVBoxView","mmui::XHBoxView",NULL};int i;for(i=0;a[i];i++)if(!strcmp(s,a[i]))return 1;return 0;}
static void attach(void *o,int kind){Record *r=record(o);void *d,*cb;if(!r||!live(r))return;d=ptr((byte*)o+0xc0);if(d==r->clone+8){r->kind=kind;return;}cb=ptr((byte*)d+8);if(cb!=image+0x40b0c80&&cb!=image+0x1eb1d40&&cb!=image+0x1c13680&&cb!=image+0xaf20)return;if(!readable((byte*)d-8,264)||ptr((byte*)o+0x160))return;r->delegate=d;r->kind=kind;memcpy(r->clone,(byte*)d-8,264);*(void**)(r->clone+16)=surfacePaint;*(void**)((byte*)o+0xc0)=r->clone+8;}
/* QObject owns posted events. PaletteChange(39) and StyleChange(100) are Qt
   widget notifications, not direct calls into unverified message implementations. */
static void notifyMessagePalette(void *o){
 Record *r;unsigned key;int n;if(!status.enabled||!mainRoot||!o)return;
 r=record(o);if(!r)return;key=status.generation*2+mode()+1;
 if(r->paletteGeneration==key)return;r->paletteGeneration=key;
 for(n=0;n<2;n++){void *e=((void*(*)(size_t))(image+0x754bcdc))(32);if(!e){status.errors++;continue;}((void(*)(void*,int))(image+0x81acb0))(e,n?100:39);((void(*)(void*,void*,int))(image+0x1581f0))(o,e,0);paletteNotifications++;}
}
static int contactBackgroundNode(void *o){
    void *v=ptr(o);
    return v==image+0x9b5e3f8||v==image+0x93add78||v==image+0x93accc8
        ||v==image+0x93a8098||v==image+0x8ffbb98||v==image+0x9833b68
        ||v==image+0x9ba4878||v==image+0x9d4c798;
}
static int contactRefreshOnly;
static void traverse(void *root){DWORD started=GetTickCount();void *queue[6000],*seenObjects[8192]={0};int front=0,end=1,rootFound=0;queue[0]=root;seenObjects[objectHash(root,8191)]=root;status.scans++;while(front<end){void *o=queue[front++],*list,*d;const char *s;int begin,finish,j,kind=0;if(!readable(o,0x168))continue;s=className(o);if(excluded(s)||(!explicitGuiRefresh&&!strcmp(s,"mmui::MessageView")))continue;d=ptr((byte*)o+8);list=ptr((byte*)d+0x18);if(readable(list,16)){begin=*(int*)((byte*)list+8);finish=*(int*)((byte*)list+12);if(begin>=0&&finish>=begin&&finish-begin<300&&readable((byte*)list+16+begin*8,(finish-begin)*8))for(j=begin;j<finish&&end<6000;j++){void *child=ptr((byte*)list+16+j*8);if(child&&(!contactRefreshOnly||contactBackgroundNode(child))){unsigned slot=objectHash(child,8191);while(seenObjects[slot]&&seenObjects[slot]!=child)slot=(slot+1)&8191;if(!seenObjects[slot]){seenObjects[slot]=child;queue[end++]=child;}}}}
{
int role=0;if(explicitGuiRefresh||!strcmp(s,"mmui::XIcon"))scope(o,&role);
if(explicitGuiRefresh&&!strcmp(s,"mmui::XTextView")&&readable(o,0x238)&&readable(ptr((byte*)o+0x28),12)&&(*(unsigned*)((byte*)ptr((byte*)o+0x28)+8)&32768)){
 Profile *p=&profiles[mode()];unsigned color=role==1?p->chat:role==2?(p->canvas?p->canvas:p->text):p->text;Record *r=record(o);
 if(r&&color){void *argument=&color;if(!r->appliedInk)r->ink=*(unsigned*)((byte*)o+0x234);r->appliedInk=color;((void(*)(void*,int,int,void*))(image+0x919a80))(o,2,5,&argument);}
}
if(explicitGuiRefresh&&role&&paletteNotifications<1024&&strcmp(s,"mmui::XTextView")&&strcmp(s,"mmui::XIcon"))notifyMessagePalette(o);
if(explicitGuiRefresh&&ptr(o)==image+0x93ef288)((void(*)(void*))(image+0x20ff6c0))(o);

if(status.paints>0&&!role&&!strcmp(s,"mmui::XIcon")&&readable(o,0x159)&&readable(ptr((byte*)o+0x28),12)&&(*(unsigned*)((byte*)ptr((byte*)o+0x28)+8)&32768)){
 Record *r=record(o);byte *svg=ptr((byte*)o+0xe8);unsigned key=status.generation*2+mode()+1;
 if(r&&readable(svg,8)&&*(int*)(svg+4)>0&&r->iconGeneration!=key){r->iconGeneration=key;*((byte*)o+0x158)=0;((void(*)(void*))(image+0x1e80420))(o);}
}
if(role)continue;
}
kind=contactSurface(o,s);if(kind>=0){attach(o,kind);}else if(allowed(s)){kind=0;if(!strcmp(s,"mmui::MainView")&&!rootFound){kind=1;rootFound=1;}else if(!strcmp(s,"mmui::ChatInputView"))kind=profiles[mode()].inputBorder?2:5;else if(!strcmp(s,"mmui::ChatSessionList"))kind=3;else if(!strcmp(s,"mmui::MainTabBar"))kind=7;attach(o,kind);}
/* The selected contact uses a separate scroll viewport over the wallpaper. */
if((ptr(o)==image+0x8ffbb98||ptr(o)==image+0x93accc8)&&profiles[mode()].wall){
    void *container=parent(o);
    int viewport=ptr(o)==image+0x8ffbb98&&ptr(container)==image+0x93a8098;
    int content=ptr(o)==image+0x93accc8&&ptr(container)==image+0x8ffbb98
        &&ptr(parent(container))==image+0x93a8098;
    if((viewport||content)&&inContacts(o)){
    byte *extra=ptr((byte*)d+0x58);
    if(readable(extra,0x70)&&(*(unsigned*)(extra+0x6c)&16)){
        Record *r=record(o);
        if(r){r->fill=1;((void(*)(void*,int))(image+0x5b6e60))(o,0);}
    }
    }
}
/* Disable only the session table viewport's automatic opaque fill. */
if(!strcmp(s,"QWidget")){void *a=parent(o);int k;for(k=0;a&&k<3;k++,a=parent(a))if(!strcmp(className(a),"mmui::ChatSessionList")||(profiles[mode()].wall&&ptr(a)==image+0x9d1e568&&inContacts(a))){byte *extra=ptr((byte*)d+0x58);if(readable(extra,0x70)&&(*(unsigned*)(extra+0x6c)&16)){Record *r=record(o);if(r){r->fill=1;((void(*)(void*,int))(image+0x5b6e60))(o,0);}}break;}}
}lastScanMs=GetTickCount()-started;if(lastScanMs>maxScanMs)maxScanMs=lastScanMs;if(lastScanMs>50)logStatus("scan-slow");}
static void restore(void){unsigned i;void *savedRoot=mainRoot;mainRoot=NULL;for(i=0;i<status.records;i++){Record *r=&records[i];if(!live(r))continue;if(r->delegate&&ptr((byte*)r->obj+0xc0)==r->clone+8)*(void**)((byte*)r->obj+0xc0)=r->delegate;if(r->fill==1)((void(*)(void*,int))(image+0x5b6e60))(r->obj,1);if(r->iconGeneration){*((byte*)r->obj+0x158)=0;((void(*)(void*))(image+0x1e80420))(r->obj);r->iconGeneration=0;}r->textGeneration=0;r->paletteGeneration=0;if(r->appliedInk&&*(unsigned*)((byte*)r->obj+0x234)==r->appliedInk)((void(*)(void*,unsigned))(image+0x1e9f7e0))(r->obj,r->ink);if(r->paragraph&&ptr(ptr((byte*)r->obj+0x3e0))==r->paragraph&&readable(r->paragraph,0x30)){((void(*)(void*,unsigned,int))(image+0x44e7c40))(r->paragraph,r->link,1);*((byte*)r->paragraph+0x29)=(*((byte*)r->paragraph+0x29)&~4)|(r->flag&4);}}
if(paletteOwned){for(i=0;i<8;i++){if(mode()==paletteMode)memcpy(image+0xbba9d30+indices[i]*24,oldPalette[i],16);else{QColor q;byte *d=oldDual[i]+mode()*4;qcolor(&q,((unsigned)d[0]<<16)|((unsigned)d[1]<<8)|d[2]);memcpy(image+0xbba9d30+indices[i]*24,&q,16);}patch(image+0xb897a60+indices[i]*8,oldDual[i],8);}paletteOwned=0;}status.active=0;mainRoot=savedRoot;}
static int selectedRow(void *o){
    int depth;
    for(depth=0;o&&depth<48;o=parent(o),depth++){
        unsigned i=recordIndex[recordSlot(o)];
        Record *r=i?&records[i-1]:NULL;
        if(r&&r->rowSelected&&live(r)&&readable(o,0x211)&&*((byte*)o+0x210)==1)return 1;
        if(o==mainRoot)break;
    }
    return 0;
}
static intptr_t textPaint(void *self,void *canvas){void *o=(byte*)self-0xc0;int chat;Profile *p=&profiles[mode()];if((!guiThread||GetCurrentThreadId()==guiThread)&&status.enabled&&mainRoot&&scope(o,&chat)){unsigned color=chat==1?p->chat:chat==2?(p->canvas?p->canvas:p->text):p->text;Record *r=record(o);if(!chat&&p->selectedText&&selectedRow(o))color=p->selectedText;else if(r&&!chat&&p->secondary){unsigned original=r->appliedInk?r->ink:*(unsigned*)((byte*)o+0x234);int rr=(original>>16)&255,gg=(original>>8)&255,bb=original&255,hi=rr>gg?rr:gg,lo=rr<gg?rr:gg;if(bb>hi)hi=bb;if(bb<lo)lo=bb;if(hi-lo<36&&hi>=65&&hi<243)color=p->secondary;}if(r&&!color&&r->appliedInk){((void(*)(void*,unsigned))(image+0x1e9f7e0))(o,r->ink);r->appliedInk=0;}if(r&&color){if(!r->appliedInk)r->ink=*(unsigned*)((byte*)o+0x234);r->appliedInk=color;if(*(unsigned*)((byte*)o+0x234)!=color)((void(*)(void*,unsigned))(image+0x1e9f7e0))(o,color);}if(chat==1&&p->link){void *para=ptr(ptr((byte*)o+0x3e0));if(readable(para,0x30)&&ptr(para)==image+0x98357b8){unsigned c=((unsigned(*)(void*))(image+0x44e7c10))(para),red=(c>>16)&255,green=(c>>8)&255,blue=c&255;if(r&&c!=p->link&&((blue>=red&&blue>=green&&red+green+blue<400)||(r->paragraph==para&&c==r->appliedLink))){if(r->paragraph!=para){r->paragraph=para;r->link=c;r->flag=*((byte*)para+0x29);}r->appliedLink=p->link;((void(*)(void*,unsigned,int))(image+0x44e7c40))(para,p->link,1);}}}}return oldText(self,canvas);}
static unsigned short clamp16(double x){return x<0?0:x>65535?65535:(unsigned short)(x+.5);}
static QColor *iconGet(void *o,QColor *out){QColor *q=oldIcon(o,out);int chat;Profile *p=&profiles[mode()];if((guiThread&&GetCurrentThreadId()!=guiThread)||!q||q->spec!=1||!status.enabled||!mainRoot||!scope(o,&chat)||protectedIcon(o))return q;if(p->iconMode==1){double v[3]={q->r,q->g,q->b},l=.2126*v[0]+.7152*v[1]+.0722*v[2];q->r=clamp16((((v[0]-l)*p->saturation/100+l-32767.5)*p->contrast/100+32767.5)*p->brightness/100);q->g=clamp16((((v[1]-l)*p->saturation/100+l-32767.5)*p->contrast/100+32767.5)*p->brightness/100);q->b=clamp16((((v[2]-l)*p->saturation/100+l-32767.5)*p->contrast/100+32767.5)*p->brightness/100);}else if(p->iconMode==2){unsigned mx=q->r>q->g?q->r:q->g,mn=q->r<q->g?q->r:q->g,c=0;if(q->b>mx)mx=q->b;if(q->b<mn)mn=q->b;if(mx-mn<0x2020)c=p->icons;else if(q->g>=q->r+0x1414&&q->g>=q->b+0xa0a)c=p->accent;if(c){unsigned short alpha=q->a;qcolor(q,c);q->a=alpha;}}if(p->iconMode)q->a=clamp16((double)q->a*(100+p->visibility)/100);return q;}
static void *rowGet(void *o,void *arg){void *result=oldRow(o,arg);int chat;if(status.enabled&&mainRoot&&readable(o,0x211)&&scope(o,&chat)){Record *r=record(o);int selected=*((byte*)o+0x210)==1;if(r)r->rowSelected=selected;if(!selected&&readable(result,16))memset(result,0,16);}return result;}
static void qstring(QString *q,QStringData *d,const wchar_t *s){size_t n=wcslen(s);memset(d,0,sizeof *d);if(n>1023)n=1023;d->ref=-1;d->len=(int)n;d->offset=24;memcpy(d->text,s,n*2);q->d=d;}
typedef struct { wchar_t path[1024]; void *sk; } ImageCache;
static ImageCache cache[64];static int imageCount;
static int imageUsed(void *sk){int m,i;for(m=0;m<2;m++){if(profiles[m].wall==sk||profiles[m].inputBorder==sk)return 1;for(i=0;i<3;i++)if(profiles[m].frost[i]==sk)return 1;for(i=0;i<5;i++)if(profiles[m].masks[i]==sk)return 1;}return 0;}
static void *decode(const wchar_t *path){int i,slot=imageCount;if(!path||!path[0])return NULL;for(i=0;i<imageCount;i++)if(!wcscmp(path,cache[i].path))return cache[i].sk;if(imageCount>=64){slot=-1;for(i=0;i<imageCount;i++)if(!imageUsed(cache[i].sk)){slot=i;break;}if(slot<0){status.errors++;return NULL;}if(readable(cache[slot].sk,16)&&InterlockedDecrement((LONG*)((byte*)cache[slot].sk+8))==0){void *fn=ptr((byte*)ptr(cache[slot].sk)+8);if(fn)((void(*)(void*))(fn))(cache[slot].sk);}cache[slot].sk=NULL;cache[slot].path[0]=0;}byte qimg[64]={0},wrapper[64]={0};QString q;QStringData d;if(!path[0])return NULL;qstring(&q,&d,path);((void(*)(void*,QString*,int))(image+0x4fd250))(qimg,&q,0);((void(*)(void*,void*))(image+0x70b60))(wrapper,qimg);((void(*)(void*))(image+0x4fdbc0))(qimg);{void *sk=*(void**)wrapper;if(sk){wcscpy(cache[slot].path,path);cache[slot].sk=sk;if(slot==imageCount)imageCount++;}return sk;}}
static unsigned readColor(const wchar_t *section,const wchar_t *key){wchar_t b[64];GetPrivateProfileStringW(section,key,L"original",b,64,configPath);if(b[0]!=L'#'||wcslen(b)!=7)return 0;return 0xff000000u|(unsigned)wcstoul(b+1,NULL,16);}
static void readAsset(const wchar_t *section,const wchar_t *key,void **dest){wchar_t p[1024];GetPrivateProfileStringW(section,key,L"",p,1024,configPath);/* Keep decoded images alive until process exit: paint callbacks may still refer to old assets. */*dest=decode(p);}
static void loadRoutes(void){wchar_t path[1100],buf[1024];size_t n=wcslen(configPath);FILE *f;char magic[8];unsigned count,len;int i,m,k,version=1,ok=1;QString tmp;wcscpy(path,configPath);while(n&&path[n-1]!=L'\\')n--;path[n]=0;wcscat(path,L"routes.bin");routeCount=0;f=_wfopen(path,L"rb");if(!f){status.errors++;return;}if(fread(magic,1,8,f)!=8||(memcmp(magic,"WXRTE001",8)&&memcmp(magic,"WXRTE002",8))||fread(&count,4,1,f)!=1||count>4096){status.errors++;fclose(f);return;}version=!memcmp(magic,"WXRTE002",8)?2:1;for(i=0;i<(int)count&&ok;i++)for(m=0;m<2&&ok;m++)for(k=0;k<2;k++){if(fread(&len,4,1,f)!=1||len>1023||fread(buf,2,len,f)!=len){ok=0;break;}buf[len]=0;if(k==0)qstring(&tmp,&routes[i].uri[m],buf);else qstring(&routes[i].qfile[m],&routes[i].file[m],buf);if(k==1){routes[i].preserveColor[m]=0;if(version==2&&(fread(&routes[i].preserveColor[m],4,1,f)!=1||routes[i].preserveColor[m]>1)){ok=0;break;}}}if(ok)routeCount=count;else status.errors++;fclose(f);}
static void reload(void){WIN32_FILE_ATTRIBUTE_DATA a;DWORD now=GetTickCount();int m,i;if(now-lastCheck<1000)return;lastCheck=now;if(!GetFileAttributesExW(configPath,GetFileExInfoStandard,&a))return;if(!CompareFileTime(&lastWrite,&a.ftLastWriteTime))return;lastWrite=a.ftLastWriteTime;status.enabled=GetPrivateProfileIntW(L"runtime",L"enabled",0,configPath)==1;if(!status.enabled){restore();status.generation++;return;}for(m=0;m<2;m++){const wchar_t *s=m?L"dark":L"light";Profile *p=&profiles[m];p->inputFull=GetPrivateProfileIntW(s,L"inputFull",0,configPath)==1;p->text=readColor(s,L"text");p->selectedText=readColor(s,L"selectedText");p->chat=readColor(s,L"chat");p->secondary=readColor(s,L"secondary");p->canvas=readColor(s,L"canvas");p->link=readColor(s,L"link");p->icons=readColor(s,L"icons");p->accent=readColor(s,L"accent");p->iconMode=GetPrivateProfileIntW(s,L"iconMode",0,configPath);p->brightness=GetPrivateProfileIntW(s,L"brightness",100,configPath);p->saturation=GetPrivateProfileIntW(s,L"saturation",100,configPath);p->contrast=GetPrivateProfileIntW(s,L"contrast",100,configPath);p->visibility=GetPrivateProfileIntW(s,L"visibility",0,configPath);readAsset(s,L"wall",&p->wall);readAsset(s,L"inputBorder",&p->inputBorder);for(i=0;i<5;i++){wchar_t k[20];swprintf(k,L"mask%d",i);readAsset(s,k,&p->masks[i]);}for(i=0;i<3;i++){wchar_t k[20];swprintf(k,L"frost%d",i);readAsset(s,k,&p->frost[i]);}}loadRoutes();for(i=0;i<16;i++){wchar_t k[20];swprintf(k,L"color%d",i);palette[i]=readColor(L"palette",k);}dirty=1;status.generation++;logStatus("config-reloaded");}
static void selectConfig(void){wchar_t next[1024],b[1024];FILE *f;size_t n;wcscpy(next,baseConfigPath);f=_wfopen(previewControl,L"rb");if(f){n=fread(b,2,1023,f);fclose(f);b[n]=0;{wchar_t *p=b[0]==0xfeff?b+1:b;if(wcslen(p)<1023&&wcsstr(p,L"native-deployment\\previews\\")&&GetFileAttributesW(p)!=INVALID_FILE_ATTRIBUTES)wcscpy(next,p);}}if(wcscmp(next,configPath)){wcscpy(configPath,next);memset(&lastWrite,0,sizeof lastWrite);lastCheck=0;dirty=1;}}
static void writeReceipt(void){wchar_t path[1100],wall[1024];FILE *f;size_t n=wcslen(configPath);int i,m=mode(),matches=0,requested;wcscpy(path,configPath);while(n&&path[n-1]!=L'\\')n--;path[n]=0;wcscat(path,L"applied.receipt");GetPrivateProfileStringW(m?L"dark":L"light",L"wall",L"",wall,1024,configPath);requested=wall[0]!=0;
for(i=0;i<8;i++){QColor q;qcolor(&q,palette[i*2+m]);if(!memcmp(image+0xbba9d30+indices[i]*24,&q,16))matches++;}
f=_wfopen(path,L"wb");if(f){fprintf(f,"state=%s\nmode=%d\ngeneration=%u\npaletteMatches=%d\nwallRequested=%d\nwallLoaded=%d\nrenderQueued=1\n",matches==8&&(!requested||profiles[m].wall)?"applied":"failed",m,status.generation,matches,requested,profiles[m].wall!=NULL);fclose(f);}logStatus("apply-receipt");}
static void CALLBACK configTick(HWND w,UINT message,UINT_PTR id,DWORD time){unsigned gen;int modeChanged,previousRefresh;if(busy||!mainRoot||ptr(mainRoot)!=image+0x983c628)return;busy=1;attachControl();selectConfig();gen=status.generation;reload();modeChanged=observeMode();if(status.enabled&&(gen!=status.generation||dirty||(status.paints>0&&uiReadyGeneration!=status.generation*2+mode()+1))){if(gen!=status.generation||!paletteOwned)paletteApply();paletteNotifications=0;previousRefresh=explicitGuiRefresh;if(gen!=status.generation||modeChanged)explicitGuiRefresh=1;traverse(mainRoot);explicitGuiRefresh=previousRefresh;if(status.paints>0)uiReadyGeneration=status.generation*2+mode()+1;dirty=0;status.active=1;((void(*)(void*))(image+0x5b7260))(mainRoot);writeReceipt();}busy=0;}
EXPORT unsigned ThemePreviewProtocol(void){return 2;}
static intptr_t mainEvent(void *o,void *e){int type=readable(e,0x12)?*(unsigned short*)((byte*)e+0x10):0;intptr_t result;if(GetCurrentThreadId()!=guiThread)return oldEvent(o,e);if(type==1000&&!busy&&InterlockedExchange(&applyPending,0)){mainRoot=o;explicitGuiRefresh=1;lastCheck=0;memset(&lastWrite,0,sizeof lastWrite);configTick(NULL,0,0,0);explicitGuiRefresh=0;return oldEvent(o,e);}if(!busy){busy=1;if(type==17||type==75||type==68)dirty=1;
/* ChildAdded/Polish can run inside constructors: mark dirty only. */
if(type==17||(type==12&&dirty)){if(type==17){logStatus("main-show-begin");mainRoot=o;attachControl();selectConfig();reload();}if(status.enabled){if(type==17||!paletteOwned)paletteApply();if(type==17)logStatus("main-palette-ready");if(dirty){traverse(o);dirty=0;}status.active=1;}if(type==17)logStatus("main-show-prepared");}
if(type==18){restore();mainRoot=NULL;dirty=1;logStatus("root-hidden-restored");}busy=0;}result=oldEvent(o,e);return result;}
static int queueApply(void){void *root=mainRoot,*event;if(!root||ptr(root)!=image+0x983c628)return 0;if(InterlockedExchange(&applyPending,1))return 1;event=((void*(*)(size_t))(image+0x754bcdc))(32);if(!event){InterlockedExchange(&applyPending,0);return 0;}((void(*)(void*,int))(image+0x81acb0))(event,1000);((void(*)(void*,void*,int))(image+0x1581f0))(root,event,0);return 1;}
/* A contact change can reuse the same widgets without a child-added event. */
static Record *contactsPaneAt(int x,int y){
    unsigned i;
    float left,top,w,h;
    if(!status.enabled||!profiles[mode()].wall||!mainRoot)return NULL;
    for(i=0;i<status.records;i++){
        Record *r=&records[i];
        if(r->vtable!=image+0x9d1e568||!live(r)||!readable(r->widgetData,12)
           ||!(*(unsigned*)((byte*)r->widgetData+8)&32768))continue;
        if(geometry(r->obj,&w,&h)&&rootPosition(r->obj,&left,&top)
           &&x>=left&&x<left+w&&y>=top&&y<top+h)return r;
    }
    return NULL;
}
static void refreshContactDetail(void){
    unsigned i;
    void (*update)(void*)=(void(*)(void*))(image+0x5b7260);
    if(busy||!mainRoot||!status.enabled||!profiles[mode()].wall)return;
    busy=1;
    for(i=0;i<status.records;i++){
        Record *r=&records[i];
        if(r->vtable!=image+0x9b5e3f8||!live(r)||!inContacts(r->obj))continue;
        contactRefreshOnly=1;
        traverse(r->obj);
        contactRefreshOnly=0;
        update(r->obj);
        break;
    }
    busy=0;
}
static LRESULT CALLBACK controlProc(HWND window,UINT message,WPARAM wparam,LPARAM lparam,UINT_PTR id,DWORD_PTR data){if(message==0x84e2)return queueApply();if(message==0x84e3){refreshContactDetail();return 0;}if(message==WM_LBUTTONUP&&contactsPaneAt((short)LOWORD(lparam),(short)HIWORD(lparam)))PostMessageW(window,0x84e3,0,0);if(message==WM_NCDESTROY){removeSubclass(window,controlProc,id);if(controlWindow==window)controlWindow=NULL;}return defaultSubclass(window,message,wparam,lparam);}
static BOOL CALLBACK findControl(HWND window,LPARAM unused){wchar_t title[32];RECT rect;GetWindowTextW(window,title,32);if(wcscmp(title,L"\x5fae\x4fe1")||GetWindow(window,GW_OWNER)||!GetClientRect(window,&rect)||rect.right<500||rect.bottom<250)return TRUE;if(!setSubclass(window,controlProc,0x57585448,0))return TRUE;controlWindow=window;return FALSE;}
static void attachControl(void){wchar_t path[1100];size_t n;FILE *f;if(controlWindow&&IsWindow(controlWindow))return;if(!setSubclass){wchar_t dll[1024];HMODULE common;UINT length=GetSystemDirectoryW(dll,980);if(!length||length>=980)return;wcscat(dll,L"\\comctl32.dll");common=LoadLibraryW(dll);if(!common)return;setSubclass=(ThemeSetSubclass)GetProcAddress(common,"SetWindowSubclass");removeSubclass=(ThemeRemoveSubclass)GetProcAddress(common,"RemoveWindowSubclass");defaultSubclass=(ThemeDefaultSubclass)GetProcAddress(common,"DefSubclassProc");if(!setSubclass||!removeSubclass||!defaultSubclass){setSubclass=NULL;return;}}EnumThreadWindows(guiThread,findControl,0);if(!controlWindow)return;wcscpy(path,baseConfigPath);n=wcslen(path);while(n&&path[n-1]!=L'\\')n--;path[n]=0;wcscat(path,L"control-ready.txt");f=_wfopen(path,L"wb");if(f){fprintf(f,"protocol=3\npid=%lu\nwindow=%llu\n",GetCurrentProcessId(),(unsigned long long)(up)controlWindow);fclose(f);}logStatus("control-ready");}
/* Contact cell paint uses a secondary interface at widget + 0x120.
   The call at 0x3da6c74 converts the row fill to ARGB; the later border
   conversion is separate. Preserve the native selected-state fill. */
static unsigned contactRowColor(void *color,void *paintInterface){
    unsigned argb=((unsigned(*)(void*))(image+0xc31be0))(color);
    void *row=(byte*)paintInterface-0x120;
    void *vt=ptr(row);
    int depth;
    if(!status.enabled||!mainRoot||GetCurrentThreadId()!=guiThread||!profiles[mode()].wall)return argb;
    if(vt!=image+0x9ddc758&&vt!=image+0x9ddbd18&&vt!=image+0x9ddc328&&vt!=image+0x9ddb8e8
       &&strcmp(className(row),"mmui::ContactsCellNewFriendView"))return argb;
    if(!readable(row,0x211)||*((byte*)row+0x210)==1)return argb;
    for(depth=0;row&&depth<10;depth++,row=parent(row))
        if(ptr(row)==image+0x9d1e568&&inContacts(row))return argb&0x00ffffff;
    return argb;
}
static int callHook(int index,up rva,void *target,void *expected){byte *site=image+rva,*stub;intptr_t delta;int j,n=0;SYSTEM_INFO si;if(site[0]!=0xe8||site+5+*(int*)(site+1)!=expected)return 0;GetSystemInfo(&si);stub=NULL;for(j=1;j<0x7000&&!stub;j++){up a=((up)site&~((up)si.dwAllocationGranularity-1))+(up)j*si.dwAllocationGranularity;stub=VirtualAlloc((void*)a,4096,MEM_RESERVE|MEM_COMMIT,PAGE_READWRITE);}if(!stub)return 0;if(index==5){stub[n++]=0x48;stub[n++]=0x89;stub[n++]=0xda;} /* mov rdx,rbx */ stub[n++]=0x48;stub[n++]=0xb8;*(void**)(stub+n)=target;n+=8;stub[n++]=0xff;stub[n++]=0xe0;{DWORD old;if(!VirtualProtect(stub,4096,PAGE_EXECUTE_READ,&old)){VirtualFree(stub,0,MEM_RELEASE);return 0;}}FlushInstructionCache(GetCurrentProcess(),stub,n);delta=stub-(site+5);if(delta<INT32_MIN||delta>INT32_MAX){VirtualFree(stub,0,MEM_RELEASE);return 0;}memcpy(callOriginal[index],site,5);callStub[index]=stub;{byte b[5]={0xe8};*(int*)(b+1)=(int)delta;return patch(site,b,5);}}
EXPORT void ThemeGetStatus(Status *out){if(out)*out=status;}
EXPORT int ThemeDisable(void){/* Must be invoked by GUI event; config is the public control channel. */return !status.enabled&&!status.active;}
EXPORT void *qt_testability_init(void){byte *slot;wchar_t path[1024];DWORD n;image=(byte*)GetModuleHandleW(L"Weixin.dll");if(!image)return NULL;/* PE size and original call targets are additional guards; launcher enforces SHA-256. */if(!readable(image,4096)||*(unsigned short*)image!=0x5a4d)return NULL;{int pe=*(int*)(image+0x3c);if(pe<0||pe>2048||*(unsigned*)(image+pe)!=0x4550||*(unsigned*)(image+pe+0x50)<0xbbd93c8)return NULL;}guiThread=GetCurrentThreadId();n=GetModuleFileNameW(own,path,1024);if(!n||n>=1000)return NULL;while(n&&path[n-1]!=L'\\')n--;path[n]=0;wcscpy(configPath,path);wcscat(configPath,L"runtime.ini");wcscpy(baseConfigPath,configPath);wcscpy(previewControl,path);wcscat(previewControl,L"preview.path");slot=image+0x983c648;oldEvent=(EventFn)ptr(slot);oldText=(PaintFn)ptr(image+0x93ab420);if(oldEvent!=(EventFn)(image+0x8fdb60)||oldText!=(PaintFn)(image+0x1ea62d0)||ptr(image+0x9883cf0)!=(void*)oldText)return NULL;oldIcon=(IconFn)(image+0x1e81480);oldRow=(RowFn)(image+0x6014740);oldSvg=(SvgFn)(image+0x19de30);{
/* Original instructions: 80803 and 808d1 pass the XIcon SVG QString (RDI).
 * 809a0 passes a local raster filename; it is NOT the other SVG branch. */
int count=0,j;up sites[5]={0x1e804b1,0x60141ad,0x1e80803,0x1e808d1,0x19da3c};void *dest[5]={iconGet,rowGet,svgGet,svgGet,assetData};void *expect[5];oldData=(DataFn)(image+0x6f860);expect[0]=oldIcon;expect[1]=oldRow;expect[2]=expect[3]=oldSvg;expect[4]=oldData;
for(j=0;j<5;j++){if(!callHook(j,sites[j],dest[j],expect[j]))break;count++;}if(count!=5){for(j=count-1;j>=0;j--)patch(image+sites[j],callOriginal[j],5);status.errors++;return NULL;}
if(!patch(image+0x93ab420,&(void*){textPaint},8)||!patch(image+0x9883cf0,&(void*){textPaint},8)||!patch(slot,&(void*){mainEvent},8)){for(j=0;j<5;j++)patch(image+sites[j],callOriginal[j],5);patch(image+0x93ab420,&oldText,8);patch(image+0x9883cf0,&oldText,8);status.errors++;return NULL;}}
if(!callHook(5,0x3da6c74,contactRowColor,image+0xc31be0))status.errors++;
installed=1;configTimer=SetTimer(NULL,0,200,configTick);status.enabled=0;logStatus("hooks-installed");return NULL;}
BOOL WINAPI DllMain(HINSTANCE h,DWORD reason,LPVOID reserved){if(reason==DLL_PROCESS_ATTACH){own=h;DisableThreadLibraryCalls(h);}return TRUE;}

