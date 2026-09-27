using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

// Runtime presentation only: combat values come from Fusion, portraits share menu IDs.
internal sealed class ArenaBattleHud
{
    readonly LocalBattleTest battle;
    readonly GameObject canvasObject;
    readonly RectTransform canvasRect;
    readonly Font font;
    readonly List<Sprite> artwork = new List<Sprite>();
    readonly Dictionary<string, Sprite> avatars = new Dictionary<string, Sprite>();
    readonly Image[] portraits = new Image[2], hpFills = new Image[2], epFills = new Image[2];
    readonly Text[] names = new Text[2], levels = new Text[2], hpLabels = new Text[2], epLabels = new Text[2];
    readonly Button[] skills = new Button[3];
    readonly Text[] cooldowns = new Text[3];
    readonly Image[] rosterHealth = new Image[5];
    readonly Text[] rosterLabels = new Text[5];
    readonly string[] rosterIds = new string[5];
    readonly RectTransform enemyCard;
    readonly Text status, resultTitle, resultDetail, rematchLabel;
    readonly GameObject settings, result;
    readonly Button rematch;
    readonly Sprite circleSprite;
    readonly Sprite panelSprite;
    readonly List<DamageLabel> floating = new List<DamageLabel>();
    struct DamageLabel { public Text label; public Vector3 world; public float start; }
    static readonly Color Purple = new Color(.64f,.85f,.96f);
    public bool SettingsOpen => settings != null && settings.activeSelf;

    public ArenaBattleHud(LocalBattleTest owner)
    {
        battle = owner;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        circleSprite = Circle();
        var panelTexture = Resources.Load<Texture2D>("BattleHUD/PastelPanel");
        if (panelTexture != null)
        {
            panelSprite = Sprite.Create(panelTexture,new Rect(0,0,panelTexture.width,panelTexture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,
                new Vector4(panelTexture.width*.16f,panelTexture.height*.28f,panelTexture.width*.16f,panelTexture.height*.28f));
            artwork.Add(panelSprite);
        }
        canvasObject = new GameObject("Arena Battle HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasRect = canvasObject.GetComponent<RectTransform>();
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280,720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        if (EventSystem.current == null)
            new GameObject("Battle Event System",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(canvasRect,false);
        LocalAccountService.TryGetCurrentAccount(out var account);
        for (int i=0;i<2;i++)
        {
            var card = Panel(canvasRect,"Player Profile",new Vector2(i,1),new Vector2(i==0?180:-205,-57),new Vector2(340,100));
            var mask = Box(card.transform,"Avatar Mask",new Vector2(.5f,.5f),new Vector2(i==0?-108:108,0),new Vector2(66,66),Color.white);
            mask.sprite = circleSprite; mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            portraits[i] = Box(mask.transform,"Menu Avatar",new Vector2(.5f,.5f),Vector2.zero,new Vector2(66,74),Color.white);
            portraits[i].sprite = Avatar(account?.avatarId);
            float x=i==0?35:-35;
            names[i]=Label(card.transform,i==0?account?.displayName??"Bạn":"Đối thủ",new Vector2(.5f,.5f),new Vector2(x,16),new Vector2(200,28),20);
            levels[i]=Label(card.transform,i==0?"BẠN · LV."+(account?.level??1).ToString("00"):"ĐANG CHỜ",new Vector2(.5f,.5f),new Vector2(x,-12),new Vector2(200,22),13);
            levels[i].color=i==0?Color.cyan:new Color(1,.45f,.55f);
        }
        BuildRoster(account);
        var controls=Panel(canvasRect,"Monster HP EP",new Vector2(.5f,0),new Vector2(0,53),new Vector2(410,98));
        Label(controls.transform,"SHADOW FOX",new Vector2(.5f,.5f),new Vector2(0,24),new Vector2(310,22),15);
        for(int i=0;i<3;i++)
        {
            int skill=i;
            var skillMask=Box(canvasRect,"Dark Skill "+i,new Vector2(1,0),new Vector2(-260+i*96,79),new Vector2(88,88),Color.white);
            skillMask.sprite=circleSprite;
            skillMask.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            skills[i]=Button(skillMask.transform,"",Vector2.zero,new Vector2(88,88),()=>battle.TryCast(0,skill));
            skills[i].image.sprite=Atlas(new Rect(82+i*507,570,360,360));
            skills[i].image.type=Image.Type.Simple;
            var key=Box(canvasRect,"Skill Key",new Vector2(1,0),new Vector2(-260+i*96,30),new Vector2(38,22),new Color(.18f,.045f,.3f,.95f));
            key.gameObject.AddComponent<Outline>().effectColor=new Color(.75f,.45f,1);
            Label(key.transform,new[]{"Q","W","E"}[i],new Vector2(.5f,.5f),Vector2.zero,new Vector2(36,22),16);
            cooldowns[i]=Label(skills[i].transform,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(70,26),20);
            cooldowns[i].gameObject.AddComponent<Outline>().effectColor=Color.black;
        }
        Bars(controls.transform,0,322,0,-23);
        var enemy=Box(canvasRect,"Enemy Overhead HP EP",new Vector2(.5f,.5f),new Vector2(240,130),new Vector2(184,32),Color.clear);
        enemyCard=enemy.rectTransform;
        Bars(enemy.transform,1,184,8,-10);
        status=Label(canvasRect,"",new Vector2(.5f,0),new Vector2(0,111),new Vector2(440,24),14);

        settings=Modal("Settings");
        var settingsCard=Panel(settings.transform,"Settings Card",new Vector2(.5f,.5f),Vector2.zero,new Vector2(380,310));
        Label(settingsCard.transform,"CÀI ĐẶT",new Vector2(.5f,.5f),new Vector2(0,110),new Vector2(270,35),26);
        Label(settingsCard.transform,"Trận online vẫn tiếp tục",new Vector2(.5f,.5f),new Vector2(0,74),new Vector2(320,24),14).color=Purple;
        Label(settingsCard.transform,"Âm thanh",new Vector2(.5f,.5f),new Vector2(0,36),new Vector2(260,24),17);
        var track=Box(settingsCard.transform,"Volume",new Vector2(.5f,.5f),new Vector2(0,5),new Vector2(260,12),new Color(.2f,.13f,.3f));
        var handle=Box(track.transform,"Handle",new Vector2(.5f,.5f),Vector2.zero,new Vector2(22,22),Purple);
        handle.sprite=circleSprite;
        var slider=track.gameObject.AddComponent<Slider>();
        track.raycastTarget=true; slider.handleRect=handle.rectTransform; slider.targetGraphic=handle;
        slider.value=PlayerPrefs.GetFloat("MonsterArena.MasterVolume",1);
        AudioListener.volume=slider.value;
        slider.onValueChanged.AddListener(v=>{ AudioListener.volume=v; PlayerPrefs.SetFloat("MonsterArena.MasterVolume",v); });
        Button(settingsCard.transform,"TIẾP TỤC",new Vector2(0,-53),new Vector2(270,43),ToggleSettings);
        Button(settingsCard.transform,"RỜI TRẬN",new Vector2(0,-105),new Vector2(270,43),battle.LeaveMatch);
        settings.SetActive(false);
        var gear=Button(canvasRect,"",Vector2.zero,new Vector2(45,45),ToggleSettings);
        Place(gear.GetComponent<RectTransform>(),canvasRect,new Vector2(1,1),new Vector2(-28,-44),new Vector2(45,45));
        gear.image.sprite=circleSprite;
        gear.image.color=new Color(.13f,.35f,.52f);
        var gearArt=new GameObject("Settings Gear",typeof(RectTransform),typeof(CanvasRenderer),typeof(ArenaHologramGraphic)).GetComponent<ArenaHologramGraphic>();
        Place(gearArt.rectTransform,gear.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(41,41));
        gearArt.gear=true;gearArt.raycastTarget=false;
        gear.image.type=Image.Type.Simple;

        result=Modal("Match Result");
        var resultCard=Panel(result.transform,"Result Card",new Vector2(.5f,.5f),Vector2.zero,new Vector2(490,260));
        resultTitle=Label(resultCard.transform,"",new Vector2(.5f,.5f),new Vector2(0,70),new Vector2(450,50),34);
        resultDetail=Label(resultCard.transform,"",new Vector2(.5f,.5f),new Vector2(0,5),new Vector2(440,50),17);
        rematch=Button(resultCard.transform,"TÁI ĐẤU",new Vector2(-112,-73),new Vector2(202,44),battle.Restart);
        rematchLabel=rematch.GetComponentInChildren<Text>();
        Button(resultCard.transform,"CÀI ĐẶT",new Vector2(112,-73),new Vector2(202,44),ToggleSettings);
        result.SetActive(false);
    }

    public void ToggleSettings()
    {
        settings.SetActive(!settings.activeSelf);
        if(settings.activeSelf) settings.transform.SetAsLastSibling();
        else PlayerPrefs.Save();
    }
    void BuildRoster(LocalAccountData account)
    {
        var heading=Panel(canvasRect,"Team Heading",new Vector2(0,1),new Vector2(69,-124),new Vector2(128,50));
        Label(heading.transform,"ĐỘI QUÁI",new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,24),14).color=Purple;
        var fox=Load("BattleHUD/ShadowFox");
        for(int i=0;i<5;i++)
        {
            rosterIds[i]=account?.teamMonsterIds!=null&&i<account.teamMonsterIds.Count?account.teamMonsterIds[i]:null;
            if(account==null&&i==0) rosterIds[i]=MonsterCatalog.StarterShadowFoxId;
            bool known=rosterIds[i]==MonsterCatalog.StarterShadowFoxId;
            var slot=Panel(canvasRect,"Team Slot "+(i+1),new Vector2(0,1),new Vector2(69,-193-i*101),new Vector2(102,100));
            if(known)
            {
                var portrait=Box(slot.transform,"Monster Portrait",new Vector2(.5f,.5f),new Vector2(0,6),new Vector2(70,68),Color.white);
                portrait.sprite=fox; portrait.preserveAspect=true;
                slot.gameObject.AddComponent<Outline>().effectColor=Color.cyan;
            }
            else Label(slot.transform,string.IsNullOrEmpty(rosterIds[i])?"—":"?",new Vector2(.5f,.5f),new Vector2(0,7),new Vector2(64,42),26).color=Purple;
            Label(slot.transform,(i+1).ToString(),new Vector2(0,1),new Vector2(16,-22),new Vector2(22,22),14);
            rosterLabels[i]=Label(slot.transform,known?"ĐANG ĐẤU":string.IsNullOrEmpty(rosterIds[i])?"TRỐNG":"CHƯA HỖ TRỢ",new Vector2(.5f,.5f),new Vector2(0,-19),new Vector2(82,18),10);
            rosterLabels[i].color=known?Color.cyan:Purple;
            rosterHealth[i]=Box(slot.transform,"Member HP",new Vector2(0,0),new Vector2(16,19),new Vector2(70,5),known?new Color(.15f,.94f,.55f):new Color(.18f,.28f,.35f));
            rosterHealth[i].rectTransform.pivot=new Vector2(0,.5f);
        }
    }
    void Bars(Transform parent,int team,float width,float hpY,float epY)
    {
        float height=team==0?21:16;
        var hp=Box(parent,"HP",new Vector2(.5f,.5f),new Vector2(0,hpY),new Vector2(width,height),new Color(.08f,.19f,.28f));
        hp.gameObject.AddComponent<Outline>().effectColor=Purple;
        hpFills[team]=Box(hp.transform,"Fill",new Vector2(0,.5f),Vector2.zero,new Vector2(width,height),team==0?new Color(.15f,.94f,.55f):new Color(.95f,.3f,.4f));
        hpFills[team].rectTransform.pivot=new Vector2(0,.5f);
        hpLabels[team]=Label(hp.transform,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(width,18),12);
        var ep=Box(parent,"EP",new Vector2(.5f,.5f),new Vector2(0,epY),new Vector2(width,12),new Color(.08f,.19f,.28f));
        ep.gameObject.AddComponent<Outline>().effectColor=Purple;
        epFills[team]=Box(ep.transform,"Fill",new Vector2(0,.5f),Vector2.zero,new Vector2(width,12),new Color(.1f,.63f,1));
        epFills[team].rectTransform.pivot=new Vector2(0,.5f);
        epLabels[team]=Label(ep.transform,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(width,16),11);
    }
    public void Refresh(int[] hp,float[,] readyAt,bool busy,bool ai)
    {
        bool ready=!battle.onlineBattle||battle.NetworkReady;
        var local=battle.LocalMonster; var opponent=battle.OpponentMonster;
        if(battle.onlineBattle&&ready) { hp=new[]{local.CurrentHealth,opponent.CurrentHealth}; busy=local.Recovering; }
        var states=new[]{local,opponent};
        for(int i=0;i<2;i++)
        {
            var state=states[i];
            bool valid=state!=null&&state.Object!=null&&state.Object.IsValid;
            if(valid)
            {
                names[i].text=state.DisplayName.ToString();
                levels[i].text=(i==0?"BẠN":"ĐỐI THỦ")+" · LV."+state.PlayerLevel.ToString("00");
                portraits[i].sprite=Avatar(state.AvatarId.ToString());
            }
            else if(i==1) { names[i].text="Đối thủ"; levels[i].text="ĐANG CHỜ"; }
            float width=i==0?322:184;
            int energy=valid?state.CurrentEnergy:NetworkMonsterMovement.MaxEnergy;
            hpFills[i].rectTransform.sizeDelta=new Vector2(width*(ready?Mathf.Clamp01(hp[i]/100f):0),i==0?21:16);
            epFills[i].rectTransform.sizeDelta=new Vector2(width*(ready?Mathf.Clamp01(energy/(float)NetworkMonsterMovement.MaxEnergy):0),12);
            hpLabels[i].text=ready?"HP "+hp[i]+" / 100":"HP — / —";
            epLabels[i].text=ready?"EP "+energy+" / 60":"EP — / —";
        }
        for(int i=0;i<5;i++)
            if(rosterIds[i]==MonsterCatalog.StarterShadowFoxId)
            {
                rosterHealth[i].rectTransform.sizeDelta=new Vector2(70*Mathf.Clamp01(hp[0]/100f),5);
                rosterLabels[i].text=hp[0]<=0?"BỊ HẠ":"ĐANG ĐẤU";
            }
        var target=battle.OpponentVisual;
        enemyCard.gameObject.SetActive(target!=null&&battle.battleCamera!=null);
        if(target!=null&&battle.battleCamera!=null)
        {
            var renderers=target.GetComponentsInChildren<Renderer>();
            Vector3 head=target.position+Vector3.up*3;
            if(renderers.Length>0)
            {
                var bounds=renderers[0].bounds;
                foreach(var r in renderers) bounds.Encapsulate(r.bounds);
                // Imported skinned bounds include unused animation reach; cap to the arena model height.
                float ground=target.parent!=null?target.parent.position.y:target.position.y;
                head=new Vector3(bounds.center.x,Mathf.Min(bounds.max.y,ground+2.8f)+.08f,bounds.center.z);
            }
            Vector3 screen=battle.battleCamera.WorldToScreenPoint(head);
            enemyCard.gameObject.SetActive(screen.z>0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,canvasObject.GetComponent<Canvas>().worldCamera,out var point);
            enemyCard.anchoredPosition=point+new Vector2(0,enemyCard.rect.height*.5f+8);
        }
        bool ended=hp[0]<=0||hp[1]<=0;
        for(int i=0;i<3;i++)
        {
            float left=battle.onlineBattle?ready?local.RemainingCooldown(i):0:Mathf.Max(0,readyAt[0,i]-Time.time);
            skills[i].interactable=ready&&!busy&&!ended&&!battle.Leaving&&!SettingsOpen&&left<=0;
            cooldowns[i].text=left>0?left.ToString("F1"):!ready?"CHỜ":busy?"…":"";
        }
        status.text=battle.onlineBattle&&(!ready||battle.Leaving)?battle.ConnectionMessage:busy?"Đang thi triển…":"";
        result.SetActive(ready&&ended&&!busy&&!battle.IsBusy);
        if(result.activeSelf) battle.PlayResultAudio(hp[0],hp[1]);
        if(ended)
        {
            resultTitle.text=hp[0]<=0&&hp[1]<=0?"HÒA":hp[1]<=0?"CHIẾN THẮNG":"THẤT BẠI";
            bool requested=ready&&battle.onlineBattle&&local.RematchRound>local.Round;
            rematch.interactable=!requested&&!battle.Leaving;
            rematchLabel.text=requested?"ĐANG CHỜ…":"TÁI ĐẤU";
            resultDetail.text=requested?"Đang chờ đối thủ đồng ý.":"Cả hai đồng ý để bắt đầu trận tiếp theo.";
        }
        for(int i=floating.Count-1;i>=0;i--)
        {
            var item=floating[i]; float t=(Time.time-item.start)/1.1f;
            if(t>=1) { Object.Destroy(item.label.gameObject); floating.RemoveAt(i); continue; }
            Vector3 screen=battle.battleCamera.WorldToScreenPoint(item.world+Vector3.up*t);
            item.label.gameObject.SetActive(screen.z>0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,canvasObject.GetComponent<Canvas>().worldCamera,out var p);
            item.label.rectTransform.anchoredPosition=p; item.label.color=new Color(1,.8f,.3f,1-t);
        }
    }
    public void ShowDamage(Vector3 world,int damage) { var label=Label(canvasRect,"-"+damage,new Vector2(.5f,.5f),Vector2.zero,new Vector2(130,50),30); label.gameObject.AddComponent<Outline>(); floating.Add(new DamageLabel{label=label,world=world,start=Time.time}); }
    public void SetVisible(bool value) { if(canvasObject!=null) canvasObject.SetActive(value); }
    public void Reset() { foreach(var item in floating) Object.Destroy(item.label.gameObject); floating.Clear(); result.SetActive(false); settings.SetActive(false); SetVisible(true); }
    public void Dispose() { Object.Destroy(canvasObject); foreach(var sprite in artwork) { if(sprite.name=="HUD Circle") Object.Destroy(sprite.texture); Object.Destroy(sprite); } }
    Sprite Avatar(string id)
    {
        if(string.IsNullOrEmpty(id)) id="avatar_pink_trainer_portrait";
        if(!avatars.TryGetValue(id,out var sprite)) { sprite=Load("BattleHUD/Avatars/"+id)??Load("BattleHUD/Avatars/avatar_pink_trainer_portrait"); avatars[id]=sprite; }
        return sprite;
    }
    Sprite Load(string path) { var t=Resources.Load<Texture2D>(path); if(t==null)return null; var s=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f)); artwork.Add(s);return s; }
    Sprite Atlas(Rect rect) { var t=Resources.Load<Texture2D>("BattleHUD/CompactAtlas"); if(t==null)return null; bool panel=rect.width>1000; float sx=t.width/1536f,sy=t.height/1024f; rect=new Rect(rect.x*sx,rect.y*sy,rect.width*sx,rect.height*sy); var s=Sprite.Create(t,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,panel?new Vector4(70*sx,35*sy,70*sx,35*sy):Vector4.zero);artwork.Add(s);return s; }
    Sprite Circle() { var t=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++)pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(32-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))));t.SetPixels(pixels);t.Apply();var s=Sprite.Create(t,new Rect(0,0,64,64),new Vector2(.5f,.5f));s.name="HUD Circle";artwork.Add(s);return s; }
    GameObject Modal(string name) { var image=Box(canvasRect,name,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color(0,0,.02f,.6f));image.rectTransform.anchorMin=Vector2.zero;image.rectTransform.anchorMax=Vector2.one;image.raycastTarget=true;return image.gameObject; }

    Image Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size)
    {
        var image=Box(parent,name,anchor,pos,size,Color.clear);
        if(panelSprite!=null)
        {
            image.sprite=panelSprite;
            image.type=Image.Type.Sliced;
            image.pixelsPerUnitMultiplier=8;
            image.color=Color.white;
            return image;
        }
        var frame=new GameObject("Holographic Glass Frame",typeof(RectTransform),typeof(CanvasRenderer),typeof(ArenaHologramGraphic)).GetComponent<ArenaHologramGraphic>();
        Place(frame.rectTransform,image.transform,new Vector2(.5f,.5f),Vector2.zero,size);
        frame.raycastTarget=false;
        return image;
    }
    static void Place(RectTransform r,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size) {r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=pos;r.sizeDelta=size;}
    Image Box(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color) {var i=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();Place(i.rectTransform,parent,anchor,pos,size);i.color=color;i.raycastTarget=false;return i;}
    Text Label(Transform parent,string value,Vector2 anchor,Vector2 pos,Vector2 size,int fs) {var t=new GameObject("Label",typeof(RectTransform),typeof(Text)).GetComponent<Text>();Place(t.rectTransform,parent,anchor,pos,size);t.font=font;t.text=value;t.fontSize=fs;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.supportRichText=false;t.raycastTarget=false;return t;}
    Button Button(Transform parent,string title,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action) {var image=title.Length>0?Panel(parent,title,new Vector2(.5f,.5f),pos,size):Box(parent,"Skill",new Vector2(.5f,.5f),pos,size,Color.white);image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.highlightedColor=new Color(.8f,.7f,1);colors.disabledColor=new Color(.4f,.4f,.5f);b.colors=colors;b.onClick.AddListener(action);if(title.Length>0)Label(image.transform,title,new Vector2(.5f,.5f),Vector2.zero,size,17);return b;}
}

// Resolution-independent UI geometry, not a recolor of the purple pet artwork.
internal sealed class ArenaHologramGraphic : MaskableGraphic
{
    public bool gear;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect=rectTransform.rect;
        Color ice=new Color(.72f,.92f,1,.98f);
        if(gear)
        {
            float radius=Mathf.Min(rect.width,rect.height)*.4f;
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;
                Line(mesh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*.65f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius*.65f,2,ice);
            }
            for(int i=0;i<8;i++) {float a=i*Mathf.PI/4;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(mesh,d*radius*.7f,d*radius,5,ice);}
            Diamond(mesh,Vector2.zero,6,new Color(.35f,.75f,1));
            return;
        }
        var points=Rounded(rect,3);
        for(int i=0;i<points.Count;i++) Triangle(mesh,rect.center,points[i],points[(i+1)%points.Count],new Color(.055f,.19f,.29f,.83f));
        Ring(mesh,points,6,new Color(.35f,.7f,1,.12f));
        Ring(mesh,points,2,ice);
        Ring(mesh,Rounded(rect,7),.8f,new Color(.4f,.76f,1,.7f));
        float r=Mathf.Min(22,rect.height*.24f);
        foreach(float side in new[]{-1f,1f})
        {
            var center=new Vector2(side*(rect.width*.5f-r-2),0);
            // Fine magical arcs at either side of the glass, kept away from labels.
            for(int i=0;i<20;i++)
            {
                float a=(-70+i*7)*Mathf.Deg2Rad,b=(-70+(i+1)*7)*Mathf.Deg2Rad;
                var p=center+new Vector2(side*Mathf.Cos(a)*r,Mathf.Sin(a)*rect.height*.43f);
                var q=center+new Vector2(side*Mathf.Cos(b)*r,Mathf.Sin(b)*rect.height*.43f);
                Line(mesh,p,q,1,new Color(.65f,.88f,1,.72f));
            }
        }
        Diamond(mesh,new Vector2(0,rect.yMax-3),5,ice);
        Diamond(mesh,new Vector2(0,rect.yMin+3),4,ice);
    }
    static List<Vector2> Rounded(Rect rect,float inset)
    {
        var list=new List<Vector2>();
        float l=rect.xMin+inset,r=rect.xMax-inset,b=rect.yMin+inset,t=rect.yMax-inset;
        float radius=Mathf.Min(20,(t-b)*.22f);
        var centers=new[]{new Vector2(r-radius,t-radius),new Vector2(l+radius,t-radius),new Vector2(l+radius,b+radius),new Vector2(r-radius,b+radius)};
        for(int corner=0;corner<4;corner++)for(int i=0;i<=10;i++){float angle=(corner*90+i*9)*Mathf.Deg2Rad;list.Add(centers[corner]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);}
        return list;
    }
    static void Ring(VertexHelper m,List<Vector2> p,float width,Color c){for(int i=0;i<p.Count;i++)Line(m,p[i],p[(i+1)%p.Count],width,c);}
    static void Triangle(VertexHelper m,Vector2 a,Vector2 b,Vector2 c,Color color){int n=m.currentVertCount;m.AddVert(a,color,Vector2.zero);m.AddVert(b,color,Vector2.zero);m.AddVert(c,color,Vector2.zero);m.AddTriangle(n,n+1,n+2);}
    static void Line(VertexHelper m,Vector2 a,Vector2 b,float w,Color c){var d=(b-a).normalized;var p=new Vector2(-d.y,d.x)*w*.5f;Triangle(m,a-p,a+p,b+p,c);Triangle(m,a-p,b+p,b-p,c);}
    static void Diamond(VertexHelper m,Vector2 p,float r,Color c){Triangle(m,p+Vector2.up*r,p+Vector2.right*r*.5f,p+Vector2.down*r,c);Triangle(m,p+Vector2.up*r,p+Vector2.down*r,p+Vector2.left*r*.5f,c);}
}


