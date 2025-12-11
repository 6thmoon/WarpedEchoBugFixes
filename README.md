As of the current game version `1.4.1` most of these issues have been fixed. However, it now incorrectly grants one-shot protection at all times, even if maximum health has been reduced by *Shaped Glass* , *Tonic Affliction* , *Shrine of Shaping* , or **Eclipse 8** curse a.k.a. permanent damage. Also, certain item and buff types are still applying their damage multipliers to both the initial hit as well as delayed ticks.

Not addressed in this update, the adjusted 10% damage reduction applies <ins>after</ins> one-shot protection calculations, effectively lowering that threshold to 81% of maximum health. It is unclear if this will be changed or is, in fact - intended. Barrier decay, while still theoretically an issue, is unlikely to be relevant without a significantly large quantity of this item.

___

~~Since release, Warped Echo has been plagued by many issues. Currently, the main problems can be summarized as follows:~~

 - ~~Ignoring armor entirely.~~
 - ~~Not resetting shield recharge or other out of combat item effects.~~
 - ~~Preventing "permanent" Eclipse 8 damage when active.~~
 - ~~Bypassing one shot protection completely.~~

~~While some of these quirks are beneficial to the player, overall it can easily be quite detrimental, even frequently leading to death. One of the goals here is to eliminate situations where someone would be better off not picking up the item. There are also other edge cases involving interactions that could occur in the case of artifacts like Vengeance and Evolution.~~

~~Therefore, for consistency's sake all damage modifiers are now applied only to the initial hit, before being split. Included in this are armor calculations, required for Oddly-Shaped Opal to work as expected, since it only applies once. This also ensures one-shot protection threshold remains accurate, as should the indicator for incoming damage.~~

~~Then, if applicable - E8 curse is incurred upon taking the delayed damage. An exception was also made for Repulsion Armor Plate, as it seems clear this is an intended synergy. Additionally, one-shot protection is now triggered as normal, after all modifiers are factored in.~~

~~However, it needed to be updated to work properly for negative values due to how this item is designed. Barrier decay is also taken into account when determining one-shot threshold; otherwise a non-lethal hit could become deadly for no other reason. The intent is that remaining health end up the same, but of course healing can offset that.~~

~~Though it would have been easier to throw out the original implementation, all of the code has been retained in one form or another. This plugin mostly consists of changes to execution order alongside supplementary logic.~~
