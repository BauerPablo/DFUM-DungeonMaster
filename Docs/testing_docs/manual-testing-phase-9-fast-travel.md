# Manual Testing Guide: Phase 9 Regional Fast Travel & Party Decision Engine

This document provides a comprehensive step-by-step verification protocol for **Phase 9 (Regional Fast Travel & Party Decision Engine)** of the Daggerfall Unity Native Multiplayer architecture.

---

## 1. Prerequisites & Compilation Check

1. **Unity Editor**: Open the project in Unity Editor.
2. **Clean Compilation Check**:
   - Verify that there are **0 compilation errors** in the Unity Console.
   - Verify that all Phase 9 network and game files compile cleanly:
     - [`NativeNetworkMessages.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/NativeNetworkMessages.cs)
     - [`DaggerfallPartyVoteWindow.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/UserInterfaceWindows/DaggerfallPartyVoteWindow.cs)
     - [`DFUNetworkManager.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/DFUNetworkManager.cs)
     - [`DaggerfallTravelPopUp.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/UserInterfaceWindows/DaggerfallTravelPopUp.cs)
     - [`DaggerfallTravelMapWindow.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/UserInterfaceWindows/DaggerfallTravelMapWindow.cs)
     - [`DaggerfallRestWindow.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/UserInterfaceWindows/DaggerfallRestWindow.cs)
3. **Setup Two Instances**:
   - **Instance A (Host)**: Unity Editor (`Play Mode`).
   - **Instance B (Client)**: Standalone Windows Player build.

---

## 2. Test Procedures

### Test Case 1: Host-Initiated Fast Travel with Unanimous Approval
* **Objective**: Confirm that when the Host initiates travel to a faraway regional destination, travel popups close cleanly, both peers receive the lateral parchment voting modal, both can vote Yes, and unanimous consensus triggers synchronized teleportation, authoritative time advancement, individual gold payment, and non-overlapping player positioning.
* **Steps**:
  1. Start Host on **Instance A** (`net_host` or `F10` -> Start Host).
  2. Connect Client on **Instance B** (`net_join 127.0.0.1` or `F10` -> Connect Client).
  3. On **Instance A (Host)**:
     - Press `V` (or click map icon) to open the Regional Travel Map.
     - Select a location within the current region or another region (e.g., Gothway Garden, Privateer's Hold, or Daggerfall City).
     - Select travel options (e.g. Cautious, Inns, Foot).
     - Click **Begin Journey**.
  4. Observe both screens:
     - The Travel Map and PopUp windows close automatically on Host.
     - The lateral parchment **Party Vote** window appears docked to the upper-right corner on **both** Host and Client.
     - The window displays:
       - Header: `[ Fast Travel ]`
       - Details: `<DestinationName> (<Days>d, <Cost>g)`
       - Live consensus tracker: `Votes: 0/2`
       - Live countdown timer: `Time: 15s`
       - Party checklist:
         - `[ ] Host (Pending)`
         - `[ ] <ClientName> (Pending)`
       - Interactive **Yes** and **No** buttons.
  5. On **Instance A (Host)**, click **Yes**:
     - Host's status changes to `[x] Host (Yes)`.
     - Counter updates to `Votes: 1/2`.
     - Host buttons hide.
  6. On **Instance B (Client)**, click **Yes**:
     - Client's status changes to `[x] <ClientName> (Yes)`.
     - Counter updates to `Votes: 2/2`.
* **Expected Result**:
  - The voting window closes on both peers.
  - HUD displays: `[Party] Traveling to <DestinationName>...`.
  - Both screens smash to black for immersion.
  - Both Host and Client are synchronously teleported via `StreamingWorld.TeleportToCoordinates` to the exact same destination city gate (deterministic gate selection based on destination coordinates).
  - **Position Spacing**: Host spawns at the primary start marker, while Client spawns offset by 1.2 meters laterally. The avatars stand shoulder-to-shoulder without collision or clipping.
  - Authoritative world time advances on Host by the journey duration and replicates to Client via `MsgWorldTimeSync`.
  - Directional sun/moon and skybox adjust to the arrival time.
  - If Cautious was selected, health, fatigue, and magicka are restored to maximum.
  - **Individual Cost Deduction**: Both Host and Client pay their own individual share of the trip cost (`DeductGoldAmount(totalCost)`). HUD prints: `[Party] Paid <Cost> gold for fast travel.` Neither player pays double or pays for the other.
  - Random enemy spawns on arrival are suppressed (`PreventEnemySpawns = true`).
  - Both screens fade smoothly back from black.

---

### Test Case 2: Client-Initiated Fast Travel with Unanimous Approval
* **Objective**: Confirm that pure clients can initiate fast travel proposals, and upon unanimous approval, the journey executes identically with both peers arriving at the identical city gate and each paying their own individual share.
* **Steps**:
  1. On **Instance B (Client)**:
     - Press `V` to open Travel Map.
     - Select a destination (e.g. Daggerfall City) and click **Begin Journey**.
  2. Observe both screens:
     - Both Host and Client receive the lateral Party Vote modal.
     - Checklist displays both members as `Pending`.
  3. On **Instance B (Client)**, click **Yes**.
  4. On **Instance A (Host)**, click **Yes**.
* **Expected Result**:
  - Unanimous consensus is reached.
  - Both players teleport together to the **identical** city gate (deterministic pseudo-random gate seed).
  - Characters spawn spaced 1.2m apart side-by-side without colliding.
  - World time advances authoritatively on Host and syncs to Client.
  - **Individual Cost Deduction**: Both Host and Client pay their individual share of the trip cost.

---

### Test Case 2B: Insolvent Member Fast Travel Rejection
* **Objective**: Confirm that if any party member cannot afford their share of the fast travel cost, they are blocked from approving and the proposal aborts cleanly.
* **Steps**:
  1. Have Client deposit or spend all gold (0 gold pieces).
  2. Initiate a paid fast travel (e.g. 25 gold inn cost) from Host.
  3. On Client, attempt to click **Yes**.
* **Expected Result**:
  - Client receives HUD message: `[Party] Cannot travel: you need <Cost> gold to pay your share (you have 0 gold).`
  - The vote is rejected (`approved = false`).
  - Party receives HUD notification: `[Party] [ Fast Travel ] declined: <ClientName> cannot afford the travel cost.`
  - No travel occurs and 0 gold is deducted from anyone.

---

### Test Case 3: Rejection / Decline Consensus (No Gold Deducted, No Teleport)
* **Objective**: Confirm that under the `Unanimous` policy, if any party member declines the travel proposal, the vote is cancelled immediately without teleportation or loss of gold.
* **Steps**:
  1. On **Instance A (Host)**, initiate fast travel to any location.
  2. On **Instance B (Client)**, click **No** on the Party Vote window.
* **Expected Result**:
  - The vote window immediately closes on both screens.
  - HUD notification appears on both instances: `[Party] <ClientName> declined the proposal.`
  - No teleportation occurs; both players remain at their current world coordinates.
  - Neither player loses any gold pieces or bank credit.
  - World time remains unchanged.

---

### Test Case 4: Vote Timeout Expiration
* **Objective**: Confirm that if party members fail to reach consensus before the 15-second timer expires, the proposal aborts safely.
* **Steps**:
  1. On **Instance A (Host)**, initiate fast travel.
  2. Do not press any buttons on **Instance B (Client)**.
  3. Wait 15 seconds while watching the live countdown `Time: 14s... 3s... 0s`.
* **Expected Result**:
  - At 0 seconds, the vote window closes on both instances.
  - HUD displays: `[Party] [ Fast Travel ] timed out.`
  - No teleportation occurs, and no gold is deducted.

---

### Test Case 5: Waiting & Loitering Consensus (Unified Party Decision System)
* **Objective**: Confirm that waiting/loitering (`DaggerfallRestWindow`) uses the same extensible `PartyVoteManager` backend, showing matching parchment messages upon completion.
* **Steps**:
  1. On **Instance A (Host)**, press `R` to open the Rest window.
  2. Click **Loiter** (or Rest) and enter `4` hours.
  3. Observe the lateral Party Vote window:
     - Header: `[ Party Loiter ]` (or `[ Party Rest ]`)
     - Details: `Loiter 4h` (or `Rest 4h`)
  4. On **Instance B (Client)**, click **Yes**.
  5. On **Instance A (Host)**, click **Yes**.
* **Expected Result**:
  - Rest window executes 4 hours authoritatively on Host.
  - Client enters immersion blackout displaying: `Loitering with the party... (4 hours)` (or `Resting with the party...`).
  - Authoritative world time advances 4 hours across both peers.
  - Upon completion:
    - **Host and Client both receive identical classic parchment popup windows**:
      - For Loitering: `You finish loitering.`
      - For Resting: `You wake up.`
    - Closing the popup pops both players cleanly back to HUD with skill gains evaluated.

---

### Test Case 6: Opt-In Consensus Policy Architecture Check (Phase 10 Foundation)
* **Objective**: Verify that the `VoteConsensusPolicy.OptIn` mechanism is ready for independent quest assignments in Phase 10 without blocking party members who decline.
* **Verification Detail**:
  - Under `VoteConsensusPolicy.Unanimous`: A single `No` vote immediately cancels the proposal for everyone.
  - Under `VoteConsensusPolicy.OptIn`: A `No` vote marks that individual player as `[!] Name (No)`, but does **not** abort the vote. When all party members have submitted their choice (or upon timeout), the action dispatches solely for players who voted `Yes`.
