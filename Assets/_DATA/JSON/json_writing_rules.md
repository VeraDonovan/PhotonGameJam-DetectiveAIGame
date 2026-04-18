# JSON Writing Rules

Use these rules when writing case JSON files.

The goal is to let the game reveal the case gradually without solving it for the player. The player should be able to reconstruct the story by combining the files, but no single entry should give away the answer.

## Overall Rules

- Separate `truth`, `evidence`, `facts`, and `npc` strictly.
- Do not mix raw clues, learned information, and conclusions in the same file.
- Do not write player deductions into player-facing data.
- Write all player-facing text in neutral detective-note language.
- Only put information in a file if it matches that file's purpose exactly.
- If a sentence explains motive, causality, guilt, or hidden meaning, it is probably too strong for player-facing files.

Use this test before writing any entry:

- If it is the hidden real answer, put it in `truth`.
- If it is a physical discoverable, put it in `evidence`.
- If it is neutral learned information, put it in `facts`.
- If it is a basic public person profile, put it in `npc`.

## `case_meta`

This file is for public case-level setup information.

Use this file for information the player or Watson can know at the start of the case.

It should contain:

- case id and title
- linked data file names
- public setting information
- public victim identity and initial body condition
- objective case summary text
- public suspect summary cards if needed
- public/objective relationship map entries

It should not contain:

- player objectives or instructions
- investigation steps
- hidden relationships
- motives
- culprit identity
- control, blackmail, abuse, or other hidden-truth relationship labels
- deductions about what the clues mean

### `caseBackground`

`caseBackground.briefBackground` is the case-panel summary text.

Write it like a short police report summary with explicit field labels. It should answer basic known setup information only:

- when
- where
- who the victim is
- where the victim was found
- what the initial death/body condition is

Required format:

- `时间：...`
- `地点：...`
- `案件详情：...`

Good case-background content:

- `时间：1994年，具体日期待核。\n地点：某北方工业城市，赵建民家中。\n案件详情：52岁的国营钢厂会计赵建民被发现死于家中。尸体带有明显酒气，头部存在致命钝器伤。`

Bad case-background content:

- `搜查住宅，收集物证，理清人物关系`
- `需要确认案发时间、来访经过、现场痕迹与死者的人际关系`
- `赵建民多年用债务、羞耻和隐秘的家庭关系操控他人`
- `这不是一起单纯的谋杀案`

### `relationshipMap`

`relationshipMap` is for public/objective relationship records only.

Use it for relationships that can be stated at case start without revealing hidden truth.

Good relationship-map content:

- `victim -> npc_1: 夫妻`
- `victim -> npc_2: 生意往来`
- `victim -> npc_3: 讨债关系`

Bad relationship-map content:

- hidden blood relations
- blackmail relationships
- abuse/control relationships
- motive relationships
- any relationship known only by the victim or one suspect

Hidden relationship records belong in `truth`, not in `case_meta`.

### Case Type Reference For AI Generation

Use this reference when generating new cases. This is authoring guidance, not player-facing text.

For the current prototype scope, generate cases in this broad format:

- one-sentence concept: a murder that happened inside a residence
- primary location: residence

Murder type tags may be combined when they do not conflict.

By scene presentation:

- homicide disguised as suicide
- conspiracy murder, with multiple perpetrators acting together

By special circumstance:

- dismemberment case
- no-body case, with the body hidden inside the residence
- rape-murder
- hired killing

By method:

- poisoning
- killing by omission, such as deliberately not feeding a dependent infant
- direct violence, such as knife attack, shooting, blunt-force attack, strangling by hand, or ligature strangulation

By motive:

- killing for money
- killing related to an affair
- revenge killing
- killing to silence a witness or cover another crime
- killing in rage after humiliation or persecution
- killing from hooligan/criminal impulse
- killing a relative to escape financial burden

## `truth`

This file is the full hidden answer key for the case.

Use this file to store the complete authored truth that the system needs to know.

It should contain:

- who the killer is
- the real motive
- the real timeline
- what physically happened
- what each suspect truly knows
- what each suspect is hiding
- hidden family relations
- cover-up actions
- ending logic or solution logic
- AI dialogue reference material such as what an NPC may reveal, what topics they resist, and optional example phrasings

Write this file directly and explicitly. This file is allowed to contain conclusions.

For dialogue-related truth data:

- organize dialogue reference information under each NPC, not as a global event list
- write per-NPC dialogue trigger points that match how the player questions that NPC
- do not store hard-required exact dialogue lines unless there is a specific authored line that must always appear
- prefer storing:
  - the trigger topic
  - what unlocks that topic
  - what the NPC can reveal
  - what the NPC avoids saying
  - optional example phrasing for AI reference

Good dialogue-reference content:

- `玩家追问案发当晚情况时，她可以承认看到过两名来访者，但不会主动解释更多关系背景`
- `如果玩家持续追问手表去向，他可以承认自己拿走了表，并强调那只表和母亲有关`
- `示例说法：见过几次，不算熟`

Bad dialogue-reference content:

- a global list of staged trigger scenes
- required exact dialogue exchanges for normal questioning
- dialogue data that is not attached to a specific NPC trigger point

Recommended structure inside each NPC truth block:

- `dialogueTriggers`

Each trigger should describe:

- `triggerId`
- `topic`
- `unlockRequirements`
- `revealGoal`
- `aiGuidance`
- `withhold`
- `examplePhrasings`

Good content:

- `林秀华在争执中使用炉钩击中死者太阳穴`
- `卫国强当晚前来索要账簿`
- `张铁柱是死者的亲生儿子`
- `案发后林秀华清理了客厅和厨房内的痕迹`

Do not worry about spoiling the case in this file. It is not the player-facing layer.

## `evidence`

This file is for physical clues only.

Use this file for things the player can physically find, inspect, collect, or receive from forensic examination.

It should contain:

- objects
- stains
- traces
- marks
- wounds
- missing-item traces
- fingerprints, blood, damage, residue
- autopsy, lab, or forensic report results

It should not contain:

- NPC dialogue
- statement summaries
- emotional reactions
- behavior descriptions
- contradictions
- motives
- conclusions

Write every entry in neutral physical language. Describe only what is there.

Good examples:

- `垃圾桶里发现一块使用过且颜色异常的抹布`
- `窗台积灰中有一块长方形空白`
- `死者太阳穴存在钝器伤`
- `抹布检出与死者一致的血迹`
- `床头灯无法正常点亮`

Bad examples:

- `张铁柱的初始说法`
- `回避尸体`
- `卫国强脸色变了`
- `有人清理过现场`
- `林秀华在撒谎`

Rule for writers:

If the clue cannot be photographed, collected, measured, or reported by forensics, do not put it in `evidence`.

## `facts`

This file is for neutral learned information.

Use this file for information the detective can record after speaking to people, comparing statements, reading reports, or reviewing physical clues.

It should contain:

- statement summaries
- neutral timeline notes
- contradictions between statements
- report-based findings stated neutrally
- public background information learned during investigation
- neutral records of what someone admitted, denied, claimed, or did

It should not contain:

- solved motives
- guilt statements
- final causal conclusions
- interpretation that tells the player what to think

Write every entry as a detective notebook entry. Record what is known, not what it means.

Good examples:

- `林秀华表示自己早上七点发现尸体`
- `卫国强承认案发当晚来过`
- `张铁柱表示是卫国强叫他来的`
- `卫国强与张铁柱的说法不一致`
- `账簿中记录了卫、张、林三人的名字`

Bad examples:

- `林秀华在撒谎`
- `卫国强是冲着账簿来的`
- `林秀华伪装了现场`
- `张铁柱偷了手表`
- `赵建民长期家暴林秀华`

Those may be true, but they are conclusions. They belong in `truth`, not in player-facing `facts`.

Rule for writers:

A `fact` can say:

- what someone said
- what someone admitted
- what two people said differently
- what a report states
- what was found in a document

A `fact` should not say:

- why someone did it
- what the clue proves
- who must be guilty
- what the player should conclude

## `npc`

This file is for basic public profile information only.

Use this file to define who each person is in simple, factual, player-readable terms.

It should contain:

- name
- gender
- age
- occupation
- public relationship to victim
- current location
- other basic known non-spoiler profile facts if necessary

It should not contain:

- dialogue
- detective opinions
- emotional interpretation
- suspicious behavior descriptions
- hidden relationships
- true motives
- secret identities
- solution-relevant truth that should be discovered later

Write this file like a case profile card, not like observation notes and not like story prose.

Good examples:

- `女，44岁，死者妻子`
- `男，35岁，个体经营者，与死者有生意往来`
- `男，27岁，无固定职业，自称来向死者讨债`

Bad examples:

- `穿着整齐，主动留在客厅，配合度较高`
- `情绪压抑，语气克制，手臂可见伤痕`
- `神情紧张，明显回避尸体方向`
- `我认识赵建民，听说出事了，就过来看看`
- `被勒索者`
- `亲生儿子`

Rule for writers:

If the information is not basic identity or profile information, it probably does not belong in `npc`.

## What Each File Is For

- `truth`: hidden full story
- `evidence`: physical clues only
- `facts`: neutral learned information
- `npc`: basic public person profile

## Final Checklist

Before finalizing an entry, ask:

- Is this the real hidden answer? Put it in `truth`.
- Is this a physical item, mark, trace, or report? Put it in `evidence`.
- Is this something learned and recorded neutrally? Put it in `facts`.
- Is this only a basic public identity or profile detail? Put it in `npc`.

If an entry contains interpretation, rewrite it more neutrally or move it to `truth`.
