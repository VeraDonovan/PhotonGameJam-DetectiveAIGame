using UnityEngine;

public class NPCAssembler : MonoBehaviour {
    public SpriteRenderer headRenderer;
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer outfitRenderer;

    public void AssembleNPC(NPCConfig config) {
        Debug.Log("进入检索阶段");
        // headRenderer.sprite = Resources.Load<Sprite>("Art/NPCs/" + config.appearance.head);
        Debug.Log("正在加载 body sprite，路径: " + config.appearance.body);
        Sprite[] bodySprites = Resources.LoadAll<Sprite>(config.appearance.body);
        int index = 4;
        if (index >= bodySprites.Length) {
         Debug.LogError("❌ 索引超出范围: index=" + index + " length=" + bodySprites.Length);
        return;
        }
        Sprite bodySprite = bodySprites[4]; // 选择第5个 sprite，假设它是正确的身体部分
        if (bodySprite == null) {
        Debug.LogError("❌ 没找到 body sprite");
        } else {
        Debug.Log("✅ 成功加载 body sprite: " + bodySprite.name);
        bodyRenderer.sprite = bodySprite;
        }
        // outfitRenderer.sprite = Resources.Load<Sprite>("Art/NPCs/" + config.appearance.outfit);
    }
}
