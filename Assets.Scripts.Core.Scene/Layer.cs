using Assets.Scripts.Core.AssetManagement;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Core.Scene
{
	public class Layer : MonoBehaviour
	{
		private const string shaderDefaultName = "MGShader/LayerShader";

		private const string shaderAlphaBlendName = "MGShader/LayerShaderAlpha";

		private const string shaderCrossfadeName = "MGShader/LayerCrossfade4";

		private const string shaderMaskedName = "MGShader/LayerMasked";

		private const string shaderMultiplyName = "MGShader/LayerMultiply";

		private const string shaderReverseZName = "MGShader/LayerShaderReverseZ";

		private Mesh mesh;

		private MeshFilter meshFilter;

		private MeshRenderer meshRenderer;

		private Material material;

		private Texture2D primary;

		private Texture2D secondary;

		private Texture2D mask;

		public string PrimaryName = string.Empty;

		public string SecondaryName = string.Empty;

		public string MaskName = string.Empty;

		private Shader shaderDefault;

		private Shader shaderAlphaBlend;

		private Shader shaderCrossfade;

		private Shader shaderMasked;

		private Shader shaderMultiply;

		private Shader shaderReverseZ;

		public int Priority;

		private int shaderType;

		public bool IsInitialized;

		public bool IsStatic;

		public bool FadingOut;

		private float startRange;

		private float targetRange;

		public Vector3 targetPosition = new Vector3(0f, 0f, 0f);

		public Vector3 targetScale = new Vector3(1f, 1f, 1f);

		public float targetAngle;

		private bool isInMotion;

		private float targetAlpha;

		private LayerAlignment alignment;

		public bool IsInUse => primary != null;

		public Material MODMaterial => material;

		public MeshRenderer MODMeshRenderer => meshRenderer;

		public string CurrentShaderName => material.shader.name;
		public float CurrentAlpha => material.GetFloat("_Alpha");

		public void RestoreScaleAndPosition(Vector3 scale, Vector3 position)
		{
			targetPosition = position;
			targetScale = scale;
			base.transform.localPosition = position;
			base.transform.localScale = scale;
		}

		private IEnumerator ControlledMotion(MtnCtrlElement[] motion)
		{
			foreach (MtnCtrlElement mt in motion)
			{
				float time = (float)mt.Time / 1000f;
				MoveLayerEx(mt.Route, mt.Points, 1f - (float)mt.Transparancy / 256f, time);
				yield return new WaitForSeconds(time);
				startRange = 1f - (float)mt.Transparancy / 256f;
			}
			FinishAll();
			if (motion[motion.Length - 1].Transparancy == 256)
			{
				HideLayer();
			}
			isInMotion = false;
		}

		public void ControlLayerMotion(MtnCtrlElement[] motions)
		{
			if (isInMotion)
			{
				base.transform.localPosition = targetPosition;
			}
			MtnCtrlElement mtnCtrlElement = motions[motions.Length - 1];
			Vector3 vector = mtnCtrlElement.Route[mtnCtrlElement.Points - 1];
			Vector3 localPosition = base.transform.localPosition;
			vector.z = localPosition.z;
			targetPosition = vector;
			targetAlpha = (float)mtnCtrlElement.Transparancy / 256f;
			GameSystem.Instance.RegisterAction(delegate
			{
				StartCoroutine(ControlledMotion(motions));
			});
		}

		public void MoveLayerEx(Vector3[] path, int points, float alpha, float time)
		{
			iTween.Stop(base.gameObject);
			Vector3[] array = new Vector3[points + 1];
			array[0] = base.transform.localPosition;
			for (int i = 0; i < points; i++)
			{
				array[i + 1] = new Vector3(
					x: path[i].x,
					y: -path[i].y,
					z: base.transform.localPosition.z
				);
			}
			if (UsingCrossShader())
			{
				alpha = 1f - alpha;
			}
			targetPosition = array[array.Length - 1];
			FadeTo(alpha, time);
			isInMotion = true;
			if (path.Length > 1)
			{
				iTween.MoveTo(base.gameObject, iTween.Hash("path", array, "movetopath", false, "time", time, "islocal", true, "easetype", iTween.EaseType.linear));
			}
			else
			{
				iTween.MoveTo(base.gameObject, iTween.Hash("position", array[1], "time", time, "islocal", true, "easetype", iTween.EaseType.linear));
			}
		}

		private static iTween.EaseType EaseTypeFromInt(int inEase)
		{
			switch (inEase) {
			case 0:
				return iTween.EaseType.linear;
			case 1:
				return iTween.EaseType.easeInOutSine;
			case 2:
				return iTween.EaseType.easeInOutSine;
			case 3:
				return iTween.EaseType.easeInOutQuad;
			case 4:
				return iTween.EaseType.easeInSine;
			case 5:
				return iTween.EaseType.easeOutSine;
			case 6:
				return iTween.EaseType.easeInQuad;
			case 7:
				return iTween.EaseType.easeOutQuad;
			case 8:
				return iTween.EaseType.easeInCubic;
			case 9:
				return iTween.EaseType.easeOutCubic;
			case 10:
				return iTween.EaseType.easeInQuart;
			case 11:
				return iTween.EaseType.easeOutQuart;
			case 12:
				return iTween.EaseType.easeInExpo;
			case 13:
				return iTween.EaseType.easeOutExpo;
			case 14:
				return iTween.EaseType.easeInExpo;
			case 15:
				return iTween.EaseType.easeOutExpo;
			default:
				return iTween.EaseType.linear;
			}
		}

		public void MoveLayer(int x, int y, int z, float alpha, int easetype, float wait, bool isBlocking, bool adjustAlpha)
		{
			float num = 1f - (float)z / 400f;
			float x2 = (float)x;
			float y2 = (float)(-y);
			Vector3 localPosition = base.transform.localPosition;
			targetPosition = new Vector3(x2, y2, localPosition.z);
			targetScale = new Vector3(num, num, 1f);
			GameSystem.Instance.RegisterAction(delegate
			{
				if (Mathf.Approximately(wait, 0f))
				{
					if (adjustAlpha)
					{
						InstantFadeTo(alpha);
					}
					FinishAll();
				}
				else
				{
					if (adjustAlpha)
					{
						if (Mathf.Approximately(alpha, 0f))
						{
							FadeOut(wait);
						}
						else
						{
							FadeTo(alpha, wait);
						}
					}
					iTween.EaseType easeType = EaseTypeFromInt(easetype);
					iTween.ScaleTo(base.gameObject, iTween.Hash("scale", targetScale, "time", wait, "islocal", true, "easetype", easeType, "oncomplete", "FinishAll", "oncompletetarget", base.gameObject));
					iTween.MoveTo(base.gameObject, iTween.Hash("position", targetPosition, "time", wait, "islocal", true, "easetype", easeType, "oncomplete", "FinishAll", "oncompletetarget", base.gameObject));
					if (isBlocking)
					{
						if (Mathf.Approximately(alpha, 0f) && adjustAlpha)
						{
							GameSystem.Instance.AddWait(new Wait(wait, WaitTypes.WaitForMove, HideLayer));
						}
						else
						{
							GameSystem.Instance.AddWait(new Wait(wait, WaitTypes.WaitForMove, FinishAll));
						}
					}
					else if (wait > 0f)
					{
						StartCoroutine(WaitThenFinish(wait));
					}
				}
			});
		}

		public IEnumerator WaitThenFinish(float time)
		{
			yield return (object)new WaitForSeconds(time);
			FinishAll();
		}

		public void FadeOutLayer(float time, bool isBlocking)
		{
			if (!(primary == null))
			{
				float current = targetRange;
				targetRange = 0f;
				targetAlpha = 0f;
				GameSystem.Instance.RegisterAction(delegate
				{
					if (Mathf.Approximately(time, 0f))
					{
						HideLayer();
					}
					else
					{
						material.shader = shaderDefault;
						current = 1f;
						FadingOut = true;
						iTween.ValueTo(base.gameObject, iTween.Hash("from", current, "to", targetRange, "time", time, "onupdate", "SetAlphaOnly", "oncomplete", "HideLayer"));
						if (isBlocking)
						{
							GameSystem.Instance.AddWait(new Wait(time, WaitTypes.WaitForMove, HideLayer));
						}
					}
				});
			}
		}

		public void DrawLayerWithMask(string textureName, string maskName, int x, int y, Vector2? origin, bool isBustshot, int style, float wait, bool isBlocking)
		{
			Texture2D texture2D = AssetManager.Instance.LoadTexture(textureName);
			Texture2D maskTexture = AssetManager.Instance.LoadTexture(maskName);
			material.shader = shaderMasked;
			SetPrimaryTexture(texture2D);
			SetMaskTexture(maskTexture);
			PrimaryName = textureName;
			MaskName = maskName;
			startRange = 0f;
			targetRange = 1f;
			targetAlpha = 1f;
			targetAngle = 0f;
			shaderType = 0;
			if (mesh == null)
			{
				alignment = LayerAlignment.AlignCenter;
				if ((x != 0 || y != 0) && !isBustshot)
				{
					alignment = LayerAlignment.AlignTopleft;
				}
				if (origin is Vector2 orig)
				{
					CreateMesh(texture2D.width, texture2D.height, orig);
				}
				else
				{
					CreateMesh(texture2D.width, texture2D.height, alignment);
				}
			}
			SetRangeOnly(startRange);
			SetAlphaOnly(targetAlpha);
			base.transform.localPosition = new Vector3((float)x, (float)(-y), (float)Priority * -0.1f);
			GameSystem.Instance.RegisterAction(delegate
			{
				meshRenderer.enabled = true;
				material.SetFloat("_Fuzziness", (style != 0) ? 0.15f : 0.45f);
				material.SetFloat("_Direction", 1f);
				FadeInLayer(wait);
				if (isBlocking)
				{
					GameSystem.Instance.AddWait(new Wait(wait, WaitTypes.WaitForMove, FinishAll));
				}
			});
		}

		public void FadeLayerWithMask(string maskName, int style, float time, bool isBlocking)
		{
			Texture2D maskTexture = AssetManager.Instance.LoadTexture(maskName);
			material.shader = shaderMasked;
			SetMaskTexture(maskTexture);
			material.SetFloat("_Fuzziness", (style != 0) ? 0.15f : 0.45f);
			material.SetFloat("_Direction", 0f);
			startRange = 1f;
			targetRange = 0f;
			targetAlpha = 0f;
			SetRangeOnly(startRange);
			GameSystem.Instance.RegisterAction(delegate
			{
				iTween.ValueTo(base.gameObject, iTween.Hash("from", startRange, "to", targetRange, "time", time, "onupdate", "SetRangeOnly", "oncomplete", "HideLayer"));
				if (isBlocking)
				{
					GameSystem.Instance.AddWait(new Wait(time, WaitTypes.WaitForMove, HideLayer));
				}
			});
		}

		public void DrawLayer(string textureName, int x, int y, int z, Vector2? origin, float alpha, bool isBustshot, int type, float wait, bool isBlocking)
		{
			Texture2D texture = null;
			if (textureName != string.Empty)
			{
				texture = AssetManager.Instance.LoadTexture(textureName);
			}
			MODDrawLayer(textureName, texture, x, y, z, origin, alpha, isBustshot, type, wait, isBlocking);
		}

		public void SetAngle(float angle, float wait)
		{
			base.transform.localRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
			targetAngle = angle;
			GameSystem.Instance.RegisterAction(delegate
			{
				if (Mathf.Approximately(wait, 0f))
				{
					base.transform.localRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
				}
				else
				{
					iTween.RotateTo(base.gameObject, iTween.Hash("z", targetAngle, "time", wait, "isLocal", true, "easetype", "linear", "oncomplete", "FinishAll"));
				}
			});
		}

		public void CrossfadeLayer(string targetImage, float wait, bool isBlocking)
		{
			Texture2D primaryTexture = AssetManager.Instance.LoadTexture(targetImage);
			material.shader = shaderCrossfade;
			SetSecondaryTexture(primary);
			SetPrimaryTexture(primaryTexture);
			PrimaryName = targetImage;
			startRange = 1f;
			targetRange = 0f;
			targetAlpha = 1f;
			SetRange(startRange);
			GameSystem.Instance.RegisterAction(delegate
			{
				if (Mathf.Approximately(wait, 0f))
				{
					FinishFade();
				}
				else
				{
					FadeInLayer(wait);
					if (isBlocking)
					{
						GameSystem.Instance.AddWait(new Wait(wait, WaitTypes.WaitForMove, FinishFade));
					}
				}
			});
		}

		public bool UsingCrossShader()
		{
			if (material.shader.name == shaderCrossfade.name)
			{
				return true;
			}
			return false;
		}

		public void SwitchToAlphaShader()
		{
			material.shader = shaderAlphaBlend;
		}

		public void SwitchToMaskedShader()
		{
			material.shader = shaderReverseZ;
		}

		public void SetPriority(int newpriority)
		{
			Priority = newpriority + 1;
			Vector3 localPosition = base.transform.localPosition;
			localPosition.z = (float)Priority * -0.1f;
			base.transform.localPosition = localPosition;
		}

		public void FadeInLayer(float time)
		{
			iTween.Stop(base.gameObject);
			iTween.ValueTo(base.gameObject, iTween.Hash("from", startRange, "to", targetRange, "time", time, "onupdate", "SetRange", "oncomplete", "FinishFade"));
		}

		public void FadeAlphaTo(float alpha, float time, string then = "FinishFadeAlpha")
		{
			iTween.Stop(base.gameObject);
			float startAlpha = targetAlpha;
			targetAlpha = alpha;
			iTween.ValueTo(base.gameObject, iTween.Hash("from", startAlpha, "to", targetAlpha, "time", time, "onupdate", "SetAlphaOnly", "oncomplete", then));
		}

		public void FadeRangeTo(float range, float time, string then = "FinishFadeRange")
		{
			iTween.Stop(base.gameObject);
			startRange = targetRange;
			targetRange = range;
			iTween.ValueTo(base.gameObject, iTween.Hash("from", startRange, "to", targetRange, "time", time, "onupdate", "SetRangeOnly", "oncomplete", then));
		}

		public void FadeTo(float alpha, float time)
		{
			if (ShaderHasRange)
			{
				FadeRangeTo(alpha, time);
			}
			else
			{
				FadeAlphaTo(alpha, time);
			}
		}

		public void InstantFadeTo(float alpha)
		{
			if (ShaderHasRange)
			{
				targetRange = alpha;
				SetRangeOnly(alpha);
			}
			else
			{
				targetAlpha = alpha;
				SetAlphaOnly(alpha);
			}
		}

		public void FadeOut(float time)
		{
			if (material.shader.name == shaderCrossfade.name)
			{
				material.shader = shaderDefault;
				startRange = 1f;
			}
			FadingOut = true;
			FadeAlphaTo(0f, time, then: "HideLayer");
		}

		public void FinishAll()
		{
			StopCoroutine("MoveLayerEx");
			iTween.Stop(base.gameObject);
			FinishFade();
			base.transform.localPosition = targetPosition;
			base.transform.localScale = targetScale;
			base.transform.localRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
		}

		public void FinishFade()
		{
			iTween.Stop(base.gameObject);
			SetRangeOnly(targetRange);
			SetAlphaOnly(targetAlpha);
		}

		public void FinishFadeAlpha()
		{
			iTween.Stop(base.gameObject);
			SetAlphaOnly(targetAlpha);
		}

		public void FinishFadeRange()
		{
			iTween.Stop(base.gameObject);
			SetRangeOnly(targetRange);
		}

		private bool ShaderHasRange {
			get
			{
				return material.shader.name == shaderCrossfade.name || material.shader.name == shaderMasked.name;
			}
		}

		public void SetRange(float a)
		{
			if (ShaderHasRange)
			{
				material.SetFloat("_Range", a);
			}
			else
			{
				material.SetFloat("_Alpha", a);
			}
		}

		public void SetRangeOnly(float range)
		{
			material.SetFloat("_Range", range);
		}

		public void SetAlphaOnly(float alpha)
		{
			material.SetFloat("_Alpha", alpha);
		}

		public void SetPrimaryTexture(Texture2D tex)
		{
			primary = tex;
			material.SetTexture("_Primary", primary);
			meshRenderer.enabled = true;
		}

		private void SetSecondaryTexture(Texture2D tex)
		{
			secondary = tex;
			material.SetTexture("_Secondary", secondary);
		}

		private void SetMaskTexture(Texture2D tex)
		{
			mask = tex;
			material.SetTexture("_Mask", mask);
		}

		public void HideLayer()
		{
			iTween.Stop(base.gameObject);
			ReleaseTextures();
			if (!IsStatic)
			{
				GameSystem.Instance.SceneController.LayerPool.ReturnLayer(this);
				GameSystem.Instance.SceneController.RemoveLayerReference(this);
			}
			targetAngle = 0f;
		}

		public void ReloadTexture()
		{
			if (PrimaryName == string.Empty)
			{
				HideLayer();
			}
			else
			{
				Texture2D texture2D = AssetManager.Instance.LoadTexture(PrimaryName);
				if (texture2D == null)
				{
					Logger.LogError("Failed to load texture " + PrimaryName);
				}
				else
				{
					SetPrimaryTexture(texture2D);
				}
			}
		}

		public void ReleaseTextures()
		{
			if (!(primary == null))
			{
				ReleaseSecondaryTexture();
				ReleaseMaskTexture();
				Object.Destroy(primary);
				primary = null;
				material.shader = shaderDefault;
				material.SetTexture("_Primary", null);
				meshRenderer.enabled = false;
				PrimaryName = string.Empty;
				SecondaryName = string.Empty;
				MaskName = string.Empty;
				Object.Destroy(mesh);
				mesh = null;
				meshFilter.mesh = null;
				FadingOut = false;
				shaderType = 0;
				targetAngle = 0f;
			}
		}

		private void ReleaseSecondaryTexture()
		{
			if (!(secondary == null))
			{
				Object.Destroy(secondary);
				secondary = null;
				SecondaryName = string.Empty;
				material.SetTexture("_Secondary", null);
			}
		}

		private void ReleaseMaskTexture()
		{
			if (!(mask == null))
			{
				Object.Destroy(mask);
				mask = null;
				MaskName = string.Empty;
				material.SetTexture("_Mask", null);
			}
		}

		private void CreateMesh(int width, int height, Vector2 origin)
		{
			int num = Mathf.Clamp(height, 1, 480);
			int num2 = num / height;
			int width2 = Mathf.RoundToInt((float)Mathf.Clamp(width, 1, num2 * width));
			mesh = MGHelper.CreateMeshWithOrigin(width2, num, origin);
			meshFilter.mesh = mesh;
		}

		private void CreateMesh(int width, int height, LayerAlignment alignment)
		{
			int num = Mathf.Clamp(height, 1, 480);
			float num2 = (float)num / (float)height;
			int width2 = Mathf.RoundToInt(Mathf.Clamp((float)width, 1f, num2 * (float)width));
			mesh = MGHelper.CreateMesh(width2, num, alignment);
			meshFilter.mesh = mesh;
		}

		public void Initialize()
		{
			shaderDefault = Shader.Find("MGShader/LayerShader");
			shaderAlphaBlend = Shader.Find("MGShader/LayerShaderAlpha");
			shaderCrossfade = Shader.Find("MGShader/LayerCrossfade4");
			shaderMasked = Shader.Find("MGShader/LayerMasked");
			shaderMultiply = Shader.Find("MGShader/LayerMultiply");
			shaderReverseZ = Shader.Find("MGShader/LayerShaderReverseZ");
			shaderType = 0;
			meshFilter = GetComponent<MeshFilter>();
			meshRenderer = GetComponent<MeshRenderer>();
			material = new Material(shaderDefault);
			meshRenderer.material = material;
			meshRenderer.enabled = false;
			targetAngle = 0f;
			IsInitialized = true;
		}

		public void Serialize(BinaryWriter br)
		{
			MGHelper.WriteVector3(br, targetPosition);
			MGHelper.WriteVector3(br, targetScale);
			br.Write(PrimaryName);
			br.Write(targetAlpha);
			br.Write((int)alignment);
			br.Write(shaderType);
		}

		private void Awake()
		{
			if (!IsInitialized)
			{
				Initialize();
			}
		}

		private void Update()
		{
		}

		public void MODDrawLayer(string textureName, Texture2D tex2d, int x, int y, int z, Vector2? origin, float alpha, bool isBustshot, int type, float wait, bool isBlocking)
		{
			FinishAll();
			if (textureName == string.Empty)
			{
				HideLayer();
			}
			else if (tex2d == null)
			{
				Logger.LogError("Failed to load texture " + textureName);
			}
			else
			{
				startRange = 0f;
				targetRange = alpha;
				targetAlpha = alpha;
				meshRenderer.enabled = true;
				shaderType = type;
				PrimaryName = textureName;
				float num = 1f;
				if (z > 0)
				{
					num = 1f - (float)z / 400f;
				}
				if (z < 0)
				{
					num = 1f + (float)z / -400f;
				}
				if (mesh == null)
				{
					alignment = LayerAlignment.AlignCenter;
					if ((x != 0 || y != 0) && !isBustshot)
					{
						alignment = LayerAlignment.AlignTopleft;
					}
					if (origin.HasValue)
					{
						CreateMesh(tex2d.width, tex2d.height, origin.GetValueOrDefault());
					}
					else
					{
						CreateMesh(tex2d.width, tex2d.height, alignment);
					}
				}
				if (primary != null)
				{
					material.shader = shaderCrossfade;
					SetSecondaryTexture(primary);
					SetPrimaryTexture(tex2d);
					startRange = 1f;
					targetRange = 0f;
					targetAlpha = 1f;
				}
				else
				{
					material.shader = shaderDefault;
					if (type == 3)
					{
						material.shader = shaderMultiply;
					}
					SetPrimaryTexture(tex2d);
				}
				SetRange(startRange);
				base.transform.localPosition = new Vector3((float)x, 0f - (float)y, (float)Priority * -0.1f);
				base.transform.localScale = new Vector3(num, num, 1f);
				targetPosition = base.transform.localPosition;
				targetScale = base.transform.localScale;
				if (Mathf.Approximately(wait, 0f))
				{
					FinishFade();
				}
				else
				{
					GameSystem.Instance.RegisterAction(delegate
					{
						FadeInLayer(wait);
						if (isBlocking)
						{
							GameSystem.Instance.AddWait(new Wait(wait, WaitTypes.WaitForMove, FinishFade));
						}
					});
				}
			}
		}
	}
}
