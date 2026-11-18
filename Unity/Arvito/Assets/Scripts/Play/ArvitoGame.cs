using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using V2 = System.Numerics.Vector2;

namespace Arvito
{
    internal enum PlayMode { Landing, Field, Bench, Ended }

    public sealed class ArvitoGame : MonoBehaviour
    {
        private const float Tick = 1f / 60f;
        private readonly Vector3 cameraOffset = new Vector3(9.2f, 14.3f, -12.5f);
        private Camera eye;
        private RenderTexture pixels;
        private Field field;
        private FieldView world;
        private FieldHud hud;
        private FieldAudio audioRig;
        private PlayMode mode;
        private float remainder;
        private Vector3 focus, groundRight, groundUp;
        private int screenWidth, screenHeight;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            eye = Camera.main;
            eye.orthographic = true;
            eye.orthographicSize = 4.2f;
            eye.allowMSAA = false;
            eye.depth = -10;
            eye.nearClipPlane = .15f;
            eye.farClipPlane = 120;
            eye.clearFlags = CameraClearFlags.SolidColor;
            eye.backgroundColor = new Color(.10f,.15f,.18f,1);
            eye.transform.rotation = Quaternion.LookRotation(-cameraOffset, Vector3.up);
            groundRight = Vector3.ProjectOnPlane(eye.transform.right,Vector3.up).normalized;
            groundUp = Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up).normalized;

            var output = new GameObject("화면 출력",typeof(Camera));
            output.transform.SetParent(transform,false);
            var display = output.GetComponent<Camera>();
            display.cullingMask = 0;
            display.clearFlags = CameraClearFlags.SolidColor;
            display.backgroundColor = Color.black;
            display.depth = 0;
            display.allowHDR = false;
            display.allowMSAA = false;
            display.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            var events = new GameObject("입력",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform,false);
            hud = new FieldHud(transform,Begin,ToggleBench,Restart);
            audioRig = new FieldAudio(gameObject);
            MakeField();
            Resize();
            CameraStep(1,true);
        }

        private void MakeField()
        {
            field = new Field();
            world = new FieldView(transform);
            field.Signal += OnSignal;
            hud.Bind(field);
            Change(PlayMode.Landing);
            world.Sync(field,eye,1);
        }

        private void Change(PlayMode next)
        {
            mode = next;
            remainder = 0;
            hud.Mode(mode);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void Begin()
        {
            if (mode != PlayMode.Landing) return;
            Change(PlayMode.Field);
            hud.Message("가까운 정비 보관함에서 E를 눌러 모듈을 회수하세요.");
        }

        private void ToggleBench()
        {
            if (mode == PlayMode.Field) Change(PlayMode.Bench);
            else if (mode == PlayMode.Bench)
            {
                if (field.Player.Rig.HasDrive) Change(PlayMode.Field);
                else hud.BenchHint("현장 복귀에는 작동 가능한 차체와 동력원이 필요합니다.");
            }
        }

        private void Restart()
        {
            if (mode != PlayMode.Ended) return;
            field.Signal -= OnSignal;
            world.Dispose();
            MakeField();
            Begin();
        }

        private void OnSignal(FieldEvent e, V2 p, bool friendly)
        {
            world.Effect(e,p,friendly);
            audioRig.Play(e,friendly,Mathf.Clamp01(1 - V2.Distance(field.Player.Position,p)/28f));
            if (e == FieldEvent.Salvage) hud.Message(field.LastRecovery);
            if (e == FieldEvent.Wave) hud.Message($"습격 {field.Wave}  ·  적 신호 감지");
        }

        private void Update()
        {
            if (screenWidth != Screen.width || screenHeight != Screen.height) Resize();
            Keyboard k = Keyboard.current;
            bool enter = k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
            if (mode == PlayMode.Landing && enter) Begin();
            else if (mode == PlayMode.Ended && enter) Restart();
            else if (k != null && (k.tabKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame)) ToggleBench();

            float dt = Mathf.Min(Time.unscaledDeltaTime,.10f);
            if (mode == PlayMode.Field)
            {
                if (k != null && k.eKey.wasPressedThisFrame) field.Interact();
                V2 move = V2.Zero;
                if (k != null)
                {
                    float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
                    float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
                    Vector3 dir = groundRight * x + groundUp * y;
                    move = new V2(dir.x,dir.z);
                }
                remainder += dt;
                while (remainder >= Tick) { field.Step(Tick,move); remainder -= Tick; }
                hud.UpdateField(groundRight,groundUp);
                if (field.Result != Outcome.Active) Change(PlayMode.Ended);
            }
            CameraStep(dt,false);
            world.Sync(field,eye,mode == PlayMode.Field ? dt : 0);
        }

        private void Resize()
        {
            screenWidth = Mathf.Max(1,Screen.width);
            screenHeight = Mathf.Max(1,Screen.height);
            int scale = Mathf.Max(1,Mathf.RoundToInt(screenHeight / 280f));
            var target = new RenderTexture(Mathf.CeilToInt((float)screenWidth / scale),
                Mathf.CeilToInt((float)screenHeight / scale),24,RenderTextureFormat.ARGB32)
                { name = "아르비토 도트 버퍼", filterMode = FilterMode.Point, antiAliasing = 1, useMipMap = false };
            target.Create();
            eye.targetTexture = target;
            hud.Pixels.texture = target;
            if (pixels != null) { pixels.Release(); Destroy(pixels); }
            pixels = target;
        }

        private void CameraStep(float dt,bool snap)
        {
            bool landing = mode == PlayMode.Landing;
            float size = landing ? 3.8f : 8.1f;
            Vector3 anchor = FieldView.World(field.Player.Position,.65f);
            if (landing) anchor -= groundRight * (size * eye.aspect * .36f);
            float weight = snap ? 1 : 1 - Mathf.Exp(-7.5f * dt);
            eye.orthographicSize = Mathf.Lerp(eye.orthographicSize,size,weight);
            focus = Vector3.Lerp(focus,anchor,weight);
            Vector3 p = focus + cameraOffset;
            float step = 2 * eye.orthographicSize / pixels.height;
            Vector3 right = eye.transform.right, up = eye.transform.up;
            float x = Vector3.Dot(p,right), y = Vector3.Dot(p,up);
            eye.transform.position = p + right * (Mathf.Round(x/step)*step-x) + up * (Mathf.Round(y/step)*step-y);
        }

        private void OnDestroy()
        {
            if (field != null) field.Signal -= OnSignal;
            if (pixels != null)
            {
                if (eye != null) eye.targetTexture = null;
                pixels.Release(); Destroy(pixels);
            }
            world?.Dispose();
            audioRig?.Dispose();
        }
    }
}
