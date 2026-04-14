using UnityEngine;
using UnityEngine.U2D; // SpriteAtlas
using DetectiveGame.Core;

public class SceneInitializer : MonoBehaviour {
    [Header("References")]
    public SpriteAtlas atlas;       // 拖你的 SpriteAtlas 进来
    public GameObject itemPrefab;   // 拖你的通用物品 Prefab 进来
    public TextAsset sceneJson;     // 拖场景 JSON 文件进来

    void Start() {
        // 解析 JSON
        SceneLayoutData layout = JsonUtility.FromJson<SceneLayoutData>(sceneJson.text);
        SceneDatabase db = SceneDatabaseBuilder.Build(layout);
        Debug.Log($"房间数量: {layout.rooms.Count}");
        foreach (var room in layout.rooms) {
        Debug.Log($"房间: {room.roomId}, 物品数量: {room.objects.Count}");
        }
        Debug.Log($"数据库房间数: {db.RoomById.Count}, 物品数: {db.ObjectById.Count}");

        // 遍历房间和物品
        foreach (var room in db.RoomById.Values) {
            foreach (var objId in db.GetObjectIdsByRoom(room.roomId)) {
                if (db.TryGetObject(objId, out var obj)) {
                    CreateItem(obj);
                }
            }
        }
    }

    void CreateItem(SceneObjectData obj) {
        // 从 Atlas 获取 Sprite
        Sprite s = atlas.GetSprite(obj.objectId);
        if (s == null) {
            Debug.LogWarning($"Sprite not found for {obj.objectId}");
            return;
        }
        Debug.Log($"Creating item: {obj.objectId}, Sprite: {s.name}");
        // 解析坐标和旋转
        Vector3 pos = ParsePosition(obj.placementPosition);
        Quaternion rot = ParseRotation(obj.placementRotation);

        // 实例化 Prefab
        GameObject go = Instantiate(itemPrefab, pos, rot);
        Debug.Log($"生成物品 {obj.objectId} 在位置 {pos}");

        go.name = obj.objectId;
        go.GetComponent<SpriteRenderer>().sprite = s;

        // 如果需要交互
        if (obj.interactive) {
            go.AddComponent<InteractiveItem>();
        }
    }

    Vector3 ParsePosition(string posStr) {
        if (string.IsNullOrEmpty(posStr)) return Vector3.zero;
        var parts = posStr.Split(',');
        return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
    }

    Quaternion ParseRotation(string rotStr) {
        if (string.IsNullOrEmpty(rotStr)) return Quaternion.identity;
        var parts = rotStr.Split(',');
        return Quaternion.Euler(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
    }
}
