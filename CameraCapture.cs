using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Threading;

public enum PaperSize {
    A0,
    A1,
    A2,
    A3,
    A4,
    A5,
    B0,
    B1,
    B2,
    B3,
    B4,
    B5,
    Custom
};

public class CameraCapture : MonoBehaviour
{
	public Camera targetCamera;
    [Range(1.0f, 1000.0f)]
    public float dpi = 350.0f;
    [HideInInspector]
    [Range(1.0f, 16.0f)]
    public float dpiToPpiRatio = 1.0f;
    [HideInInspector]
    public int maximumPixels = 18000000;
    [HideInInspector]
    [Range(1.0f, 2000.0f)]
    public float paperWidth = 210.0f;
    [HideInInspector]
    [Range(1.0f, 2000.0f)]
    public float paperHeight = 297.0f;
    [HideInInspector]
    public bool swap = false;
    [HideInInspector]
    public PaperSize paperSize = PaperSize.A4;
    [HideInInspector]
    public String statusText = "";
    [HideInInspector]
    public bool captureFlag = false;
    private bool beforeCaptureFlag = false;
    private bool isPlaying = false;

    private double inchPerMilli = 1.0 / 25.4;

    private int[] paperWidthArray = new int[] {
        841,
        594,
        420,
        297,
        210,
        148,
        1030,
        728,
        515,
        364,
        257,
        182
    };

    private int[] paperHeightArray = new int[] {
        1189,
        841,
        594,
        420,
        297,
        210,
        1456,
        1030,
        728,
        515,
        364,
        257
    };

    private SynchronizationContext context = SynchronizationContext.Current;

    void OnGUI() {
        isPlaying = EditorApplication.isPlaying;
    }

    public IEnumerator capture () {
		if (!isPlaying) {
			statusText = "プレイ中ではないのでキャプチャできません！";
            captureFlag = false;
			yield break;
		}
        double paperWidthTemp = paperWidth * inchPerMilli;
        double paperHeightTemp = paperHeight * inchPerMilli;
        if (paperSize != PaperSize.Custom) {
            paperWidthTemp = (double)paperWidthArray[(int)paperSize] * inchPerMilli;
            paperHeightTemp = (double)paperHeightArray[(int)paperSize] * inchPerMilli;
        }
        if (swap) {
            double temp = paperWidthTemp;
            paperWidthTemp = paperHeightTemp;
            paperHeightTemp = temp;
        }
        int width = (int)Math.Floor(dpi / Math.Sqrt(dpiToPpiRatio) * paperWidthTemp);
        int height = (int)Math.Floor(dpi / Math.Sqrt(dpiToPpiRatio) * paperHeightTemp);
        if (width * height > maximumPixels) {
			statusText = "最大ピクセル数を超えています！";
            captureFlag = false;
			yield break;
		}
        if (targetCamera.targetTexture != null ) targetCamera.targetTexture.Release();
        RenderTexture renderTex = new RenderTexture(width, height, 24);
        Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        statusText = "レンダリングしてTexture2Dに変換中…";
        yield return new WaitForEndOfFrame();
        RenderTexture.active = renderTex;
        targetCamera.targetTexture = renderTex;
		targetCamera.Render();
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        targetCamera.targetTexture = null;
        Destroy(renderTex);
        statusText = "PNGにエンコード中…";
        yield return new WaitForEndOfFrame();
        byte[] bytes = tex.EncodeToPNG();
        statusText = "保存中…";
        yield return new WaitForEndOfFrame();
        String fileName = "Saved-" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
        File.WriteAllBytes(Application.dataPath + "/../Assets/" + fileName, bytes);
        Destroy(tex);
        statusText = "キャプチャしました！ width: " + width + " height: " + height;
        captureFlag = false;
	}

    public void startCapture () {
        StartCoroutine(capture());
    }

    void Update () {
        if (beforeCaptureFlag && captureFlag == false) {
            beforeCaptureFlag = false;
        } else if (beforeCaptureFlag == false && captureFlag) {
            beforeCaptureFlag = true;
            startCapture();
        }
    }
}

[CustomEditor(typeof(CameraCapture))]
public class CameraCaptureEditor : Editor {
    private bool disabled;
    public override void OnInspectorGUI () {
        base.OnInspectorGUI ();

        CameraCapture cameraCapture = target as CameraCapture;
        cameraCapture.dpiToPpiRatio = EditorGUILayout.FloatField(
            "DPI/PPI比", cameraCapture.dpiToPpiRatio);
        cameraCapture.swap = EditorGUILayout.Toggle("幅と高さを入れ替える", cameraCapture.swap);
        cameraCapture.paperSize = (PaperSize)EditorGUILayout.EnumPopup(
            "用紙サイズ", cameraCapture.paperSize);
        disabled = cameraCapture.paperSize != PaperSize.Custom;
        EditorGUI.BeginDisabledGroup(disabled);
        cameraCapture.paperWidth = EditorGUILayout.FloatField(
            "幅（mm）", cameraCapture.paperWidth);
        cameraCapture.paperHeight = EditorGUILayout.FloatField(
            "高さ（mm）", cameraCapture.paperHeight);
        EditorGUI.EndDisabledGroup();
        cameraCapture.maximumPixels = EditorGUILayout.IntField(
            "最大ピクセル数", cameraCapture.maximumPixels);
        EditorGUILayout.LabelField("ステータス", cameraCapture.statusText);
        if (GUILayout.Button("キャプチャ")) cameraCapture.captureFlag = true;
    }
}
