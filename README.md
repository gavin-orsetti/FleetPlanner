# Overview
***
This is a light weight star citizen fleet planner that helps its user determine what ships they should spend real money on, and which ones they should work towards in game by helping them clarify the purpose and value add of any potential additions to their fleet. It aims to help the user align their purchases with their playstyle, and maximize the utility of their dollars. It does not try to replace (or even necessarily compete with) any of the existing fleet planner tools (starship42, ccu chain, Erkul, etc.), but instead aims to provide users with a foundation upon which to build their use of those tools.

## Elevator Pitch
***
Fleet Planner is a lightweight, mobile and desktop application for purposeful, preferred gameplay aligned, in-game and real money ship purchase planning. It helps to ensure ships purchased with real money align with the actual in game value the user will get out of the ship.
## Minimum Viable Product: Feature List
***

| Feature       | Purpose                                                                                                                                                                                                        |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Fleet         | View and edit every ship you plan to own (regardless of purchase state or currency)                                                                                                                            |
| Task Group    | assign objectives to specific ships in the fleet                                                                                                                                                               |
| Ship Database | A list of all the currently announced ships with a selection of significant details about their size, role, price, crew count, etc. that is referenced when creating a new ship detail page.                                                                                                                                  |
| My Ships      | See what ships you currently own and the method of purchase                                                                                                                                                    |
| Shopping List | View un-purchased ships and the intended method of purchase                                                                                                                                                    |
| Ship Detail   | Assign a usage, significance, personal attachment rating (PAR), and synergies to a specific ship, along with viewing the purchase price and currency of the ship (including melt value if purchased with cash) |

## Minimum Viable Product: Detailed Description
***
### Home (Fleets)
Upon opening the app the user will see the Fleet page. From here they can create a fleet. When creating a new fleet, only the most basic information about the fleets name, area of operation, purpose, and affiliation is collected. This decision was made because the fleet>task group> ship structure of the app quickly becomes quite complex. Requiring users to flesh out their entire fleet during the initial creation process would be far too cumbersome and is more likely to discourage users from thinking deeply about their fleet choices. By making each step of the process a stand alone task, users are able to focus on one thing at a time, first creating a fleet with a well thought out purpose, then moving on to breaking the purpose of that fleet up into clearly defined task groups, and, finally, fleshing out the finer details of that task group's operation through each ships detail page. This is also why, if the user adds multiple of a single ship, each one is added to the fleet as an individual item and each has its own detail page, rather than just having an "x3" label next to ships with multiples. The reasoning for this is that every ship will be an individual item in game, and the user should have justification for why they want/need multiples. If they cannot explain to themselves the purpose of the nth ship, they shouldn't be including it in their fleet.

### Task Group
On the page users can assign specific ship to a common objective and make notes about how they are envisioned to achieve that objective within the confines of the fleet manifesto. An example might be the mining ships from a fleet. They are tasked with gathering a specific resource from a specific area and may have restrictions placed upon them such as the total quantity gathered (in order to synergize with the refining or transport capabilities of the fleet), or the maximum signature levels allowed (in order to comply with a stealth or subterfuge objective of the manifesto). This page, unlike the synergies and ship detail pages, is about coordinating many ships together to maximize their shared potential for a common purpose by setting boundaries and limitation on their individual usage, rather than exploring the maximum limits of their individual potential.

### Ship Detail
Tapping/clicking on any ship in either the "My Ships" or "Shopping List" pages will take them to the "Ship Detail" page. From here the user can name their ship, write notes about its usage and significance, change the purchase currency and melt value, and add/view synergies, set player and NPC crew numbers, assign a personal attachment rating (i.e. I just love/hate this ship), create a hourly balance sheet, and flesh out their thoughts on how best to utilize the ships potential.

### Shopping List
From the "Shopping List" page the user can see what ships they plan on adding to their fleet, but have not yet purchased. They can see the intended purchase method and mark off ships as they are purchased (this will add them to the "My Ships" page). 

## Custom Controls
***
Everything I was going to put here has been solved by using UraniumUI.



## Roadmap
***

### Synergies
- Add Synergies and Synergy view to the app
### Fleets
### Task Groups
- Use discovered Synergies to make Task Group Ship recommendations based on what ships have already been added to the task group.
- Add a "Task Type" dropdown to the Task Group creator and make ship recommendations to the user based on the ship types required for completing that task.
### Ship Detail
- Add images to the ships (https://starcitizen.tools/Category:Ship_images)
- Use integrality and attachment rating with the cash value of ships to make aUEC/Cash buy suggestions in the shipDetail view
- Add an option to link to a specific erkul loadout so that users can (nearly) seamlessly zoom in on the fine details of their fleet.
- Find out if anyone has created a tool for measuring the fuel and resource consumption of a ship based on its components and usage, and link to it if it exists, otherwise perhaps consider creating it ourselves. (Erkul might already do this, haven't used the tool in a while).
- Add a "Cut" payment method so that users can calculate profits based on a split percentage rather than an hourly rate per crew member.
- Add a "state" field where user's can display if the ship is functional, in for repairs, destroyed, stolen, etc. and easily see if there is a need to re-task other ships to fill the gaps.
- Add a "Create New" flow when re-tasking a ship, so that users can move a ship to a new task group without needing to create it first.
- Add a "Buy Priority" field that allows players to rank on a scale of 1 (low priority) to 10 (maximum priority) how urgently their fleet needs the ship.
### Shopping List
- Use integrality and attachment ratings to make buy/melt suggestions on the shopping list view
- Find out if there is a way to get live ship data (Price (auec & cash), Buy location, Availability, etc.) from SC tools or any of the other datamining platforms using an api or something.
- Add a "melt and buy instead" and "melt and upgrade" category to the shopping list view.
- Allow users to sort their fleet shopping lists based on the "buy priority" of each ship.
- Allow users to sort the fleet view of the shopping lists based on the cumulative "buy priority" of the ships it contains (resulting in the fleet with the most, most important, ships being listed first).

### Potential Feature Details
***
### Ship Usage Summary
One or more graphical representations of the ships the user has selected to include across all fleets. The aim of this is to give the user a visually accessible way to determine if they have a particular preference for, or aversion to, a specific ship (or class of ship) and either address or capitalize on it. The intention is that it would provide the user with a way to spot holes in their fleet design, diversify if they have an over reliance on a specific ship (using 10 vultures instead of a Reclaimer) or consolidate if they have an eclectic selection of ships to accomplish a task where one or more of a single ship chassis could get the job done. This view could one day be augmented with suggestions and opinions regarding fleet design (if that is deemed desirable based on user feedback), but initially would be designed to be informational only, not trying to influence the user's choices (the more enjoyable experience in my mind).

### Fleet/Task Group Cost (& other) Visualizations
From the My Ships page the user can see the total cost of what they currently own in both USD (plus a conversion to their selected currency) and AUEC. They can also, optionally see how much it would have cost them to acquire their entire fleet in either currency. This would be added as an entry to the Fleet and Task Group views, so that users can, at a glance, see if they have one collection of ships which is significantly more expensive that others. This could be paired with the Integrality and PAR data associated with each Ship Detail/Task Group to come up with a value ratio that shows the user if they are spending their money (real or in-game) on the ships that are really useful/really loved, or if they have spent most of their money on ships they don't find much joy or utility in. Other useful visualizations could be added based on user feedback and demand.

### Synergies Page
Synergies allow you to place two ships inline with each other and make a note about how you envision them working well together. This data can then be referenced and surfaced by other pages in which one of the ships is present. The idea is that this would form the basis of an agnostic recommendation engine, which only makes recommendations based on the user's own considerations, and therefore shouldn't ever push them to construct or design ship groupings that run counter to their preferences. The notes the user associates with each pairing could one day include tags that allow them to influence how and when the recommendation engine surfaces that particular pairing.

### Fleet/Task Group Budget
A line item to assign a budget (real and/or in-game currency) to a given collection of ships which is compared with the purchase price (warbond or in-game) of each of the ships contained in that collection. If the value of the collection of ships exceeds the budget, a warning can be displayed to remind the user that they were trying to stick within specific constraints, and if the budget exceeds the value of the collection (by a significant margin), a different warning could be displayed that suggests the user may have over budgeted (or not completed the planning) for that specific collection of ships.

### Fleet/Task Group Designer Page
This is essentially bringing the concept of an interface to the fleet planning world. On this page you would spec out a collection of requirements for completing a specific task, things like the Minimum SCU, Bespoke Equipment, Hardpoint Size, Ship Size, Role, Crew Count etc. This 'interface' can then be applied to a collection of ships (Fleet or Task Group) which can then only contain ships that meet one or more of the requirements in the interface and *must*, cumulatively, meet all the requirements. Ships which meet more of the requirements would be ranked higher in the search results and ships which meet none of the requirements would be excluded. Unlike interfaces, collections of ships would only be able to adhere to one design spec, so no inheritance.