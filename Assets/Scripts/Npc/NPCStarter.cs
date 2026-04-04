using UnityEngine;

public class NPCStarter : MonoBehaviour {
    public NPCAssembler assembler;

    void Start() {
        Debug.Log("NPCStarter Start 被调用了");
        NPCConfig config = NPCLoader.LoadNPCConfig("npc.json");
        assembler.AssembleNPC(config);
    }
}
