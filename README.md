# Overview
***
This is a light weight star citizen fleet planner that helps its user determine what ships they should spend real money on, and which ones they should work towards in game. It aims to help the user align their purchases with their playstyle, and maximize the utility of their dollars. It does not try to replace (or even necessarily compete with) any of the existing fleet planner tools (starship42, ccu chain, etc.), but instead aims to provide users with a foundation upon which to build their use of those tools.

## Elevator Pitch
***
Fleet Planner is a lightweight, mobile and desktop application for purposeful, gameplay aligned, in-game and real money ship purchase and utilization planning. It helps to ensure ships purchased with real money align with the actual in game value the user will get out of the ship.

## Minimum Viable Product: Feature List
***

| Feature        | Purpose                                                                                                                          |
| -------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| Fleet          | View and edit every ship you plan to own (regardless of purchase state or currency)    
| Task Group | assign objectives to specific ships in the fleet|
| Ship Browse    | Browse a list of all the currently announced ships and add them to your fleet                                                                |
| My Ships       | See what ships you currently own and the method of purchase                                                                      |
| Shopping List  | View un-purchased ships and the intended method of purchase                                                                      |
| Ship Detail    | Assign a usage, significance, personal attachment rating (PAR), and synergies to a specific ship, along with viewing the purchase price and currency of the ship (including melt value if purchased with cash)|

## Minimum Viable Product: Detailed Description
***
### Home (Fleets)
Upon opening the app the user will see the Fleet page. From here they can create a fleet, and browse to add any ships they own (or plan to own). When adding, a pop up is shown that allows the user to customize the quantity of that ship that should be added. along with two drop downs to select if the ship is owned or not, and if it is owned, how it was purchased (in game or real money). If the user adds multiple of a single ship, each one is added to the fleet as an individual item and each has its own detail page. The reasoning for this is that every ship will be an individual item in game, and the user should have justification for why they want/need multiples. If they cannot explain to themselves the purpose of the nth ship, they shouldn't be including it in their fleet.

### Task Group
On the page users can assign specific ship to a common objective and make notes about how they are envisioned to achieve that objective within the confines of the fleet manifesto. An example might be the mining ships from a fleet. They are tasked with gathering a specific resource from a specific area and may have restrictions placed upon them such as the total quantity gathered (in order to synergize with the refining or transport capabilities of the fleet), or the maximum signature levels allowed (in order to comply with a stealth or subterfuge objective of the manifesto). This page, unlike the synergies and ship detail pages, is about co-ordinating many ships together to maximize their shared potential for a common purpose by setting boundaries and limitation on their individual usage, rather than exploring the maximum limits of their individual potential.

### Ship Browse
Once the user has added one or more ships to their fleet the other pages (My Ships, Shopping List, Ship Synergies) are created for their fleet.

### My Ships
From the My Ships page the user can see the total cost of what they currently own in both USD (plus a conversion to their selected currency) and AUEC. They can also, optionally see how much it would have cost them to acquire their entire fleet in either currency. 

### Shopping List
From the "Shopping List" page the user can see what ships they plan on adding to their fleet, but have not yet purchased. They can see the intended purchase method and mark off ships as they are purchased (this will add them to the "My Ships" page). 

### Ship Detail
Tapping/clicking on any ship in either the "My Ships" or "Shopping List" pages will take them to the "Ship Detail" page. From here the user can name their ship, write notes about its usage and significance, change the purchase currency and melt value, and add/view synergies, set player and NPC crew numbers, assign a personal attachment rating (i.e. I just love/hate this ship), create a hourly balance sheet, and flesh out their thoughts on how best to utilize the ships potential.

### Synergies
Synergies allow you to place two ships inline with each other and make a note about how you envision them working well together.

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
