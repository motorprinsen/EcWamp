# Views and Subscriptions in the EOS API

## Views

### In the EOSFunction

We register/implement a new RPC in the EOSFunction for getting views (`se.rssoftware.eos.getView`) with the following signature:

`JsonDataSet GetView(string area, string viewFile, object[] args);`

| Argument | Description                                                  |
| -------- | ------------------------------------------------------------ |
| area     | The area in which the view resides                           |
| viewFile | The relative/virtual path to the view file.                  |
| args     | The arguments to pass to the view.<br />This is basically the argument list from the `OnManeuver` attribute for the tab.<br /><br />**Note!** We need to check/handle when all file arguments are `(Default)`. In that case, they're not included in `OnManeuver` but perhaps that doesn't matter? |

The procedure uses ExoConfig to parse the view file (and adding defaults etc). We then convert the EcDataSet we get into a JsonDataSet:

#### JsonDataSet

| Name   | Type                                     | Description                           |
| ------ | ---------------------------------------- | ------------------------------------- |
| Result | [JsonDataSetResults](#jsondatasetresult) | The result of the ExoConfig operation |
| Format | string                                   | The format                            |
| Nodes  | List<[JsonDataNode](#jsondatanode)>                       | A list of nodes/children              |

#### JsonDataNode

| Name       | Type                                | Description                                    |
| ---------- | ----------------------------------- | ---------------------------------------------- |
| Type       | string                              | The type of node                               |
| Children   | List<[JsonDataNode](#jsondatanode)> | A list of nodes/children                       |
| Attributes | IDictionary<string, object>         | A dictionary of all the attributes of the node |

#### JsonDataSetResults

| Name         |
| ------------ |
| Success      |
| FileNotFound |
| BadFormat    |
| Error        |
| Empty        |

Since this implementation is aimed at creating a lean runtime we strip away the ArgumentsFolder, and we also do not include any of the ExDataProperties.
*Once we feel the need for them (when implementing a design-time mode) it's easy to enable them again.*

### In the API

First, we save a copy of the (complete) view for later reuse (caching it).

We the filter out (remove) any elements (and their children) based on the access level of the user requesting the view.
Then all the nodes are re-hung/re-arranged to better fit the final blob format. Or is this done in a different conversion step, Danne?

After that we traverse the dataset and find all variable bindings. These are put in a "user+view"-specific list (which is later added to the Master List) and we then read their values using the existing Read RPC in the EOSFunction.
At the same time (when traversing) we save the position/path of all bound elements (with a reference to its technical variable name) for later use when getting element updates.

Once all the initial values has been read, the dataset is converted into its final blob appearance and sent back to the client.

## Subscriptions

### `subscriptionId`'s and variable lists

User A requests View X, which contains 5 variables in total.
Due to access restrictions User A only sees two of the variables.

User B requests the same View but has access to all variables:

| Variable                | Visible to A | Visible to B |
| ----------------------- | ------------ | ------------ |
| Controller1.OutdoorTemp | Yes          | Yes          |
| Controller1.FanMode     | No           | Yes          |
| Controller1.RoomTemp    | No           | Yes          |
| Controller1.Setpoint    | Yes          | Yes          |
| Controller1.EAFSpeed    | No           | Yes          |

#### Alt 1

When variables are found (during view processing) we create a unique `subscriptionId` which lets us (somehow) re-map the two variables at a later time (when the `subscriptionId` is used for setting up an actual subscription).
But for how long should the id be unique? Forever? And how do we reverse the id back to the two variables?
The `tabId` is unique (Folder.name + Tab.name) and lets us resolve it into something useful. So perhaps the `subscriptionId` should be `tabId` + username? By doing so we can easily resolve the id into a specific tab (with specific arguments) and then filter out any "unauthorized" variables (based on the username). Is it a drawback that the `subscriptionId` is reusable and not truly unique?

#### ~~Alt 2~~

~~When these two variables are found (during view processing) we create a UserSubscription object which contains the variables, and the object itself is identified using a ShortGuid which is also the `subscriptionId` we return.
But how do we then know when to delete this object? The `subscriptionId` must be valid as long as the user is logged in. Or even longer? Forever?~~

~~And we also need a HashSet to map the guids to actual variable lists (and the view as well?) so we can set up DataStore subscriptions once the user decides to start a subscription.
The same question applies here; how do we know when to delete an entry for the HashSet, or does it live on until restart?~~

#### ~~Alt 3~~

~~When these two variables are found (during view processing) they are put in the "Master Variable List", containing all currently active advises (in the DataStore) (or should we wait until they are actually requested?)~~

~~The `subscriptionId` is the `viewId` (or `tabId`?) which can identify a specific view (with arguments). And since the variables are always "filtered" per user we get a unique list.~~

### Subscription flow

#### Alt 1

User A request a subscription using a previously acquired `subscriptionId` and the GraphQL subscription resolver extracts the `sid` and the current username (from the sid?). Based on this information we can:

- Locate the correct area and tab
  - Apply the correct arguments on the tab
- Load the view file (with arguments)
- Filter out unauthorized elements in the view
- Create a subscription list containing the remaining variables

(we need to keep the filtered view in memory since the updates will be path based!)

Using the subscription list we can let the VariableService create a unique VariableStream for the `sid`.
The VariableService keeps an internal list of all created (and active) VariableStreams. It (or the DataStore?) also keeps a distinct "Master List" of all the currently active variables (which probably needs some sore of reference counter (to know when to unadvise variables)).

When the new VariableStream is created the VariableService adds (to the Master List) any variables not yet present (in the Master List), and increases the ref counter on already present variables.

New values from the DataStore updates the corresponding variables in the Master List, which in turn notifies the affected VariableStreams.

Once a VariableStream detects that it doesn't have an observer (i.e. the user navigated away/closed the bowser (the WebSocket stream disconnected)) it signals the VariableService to remove that instance. The VariableService then decreases the ref counters (in the Master List) for the variables of that specific stream and unadvises (or let them die out?) the variables with a zero-count and removes the VariableStream instance from its internal list.

### Data structures

#### Alt 1

##### Variable*Something*

| Field          | Type                | Description                 |
| -------------- | ------------------- | ---------------------- |
| subscriptionId |                     | The subscriptionId.           |
| view           | RuntimeView or View | A reference to the underlying view. This is needed because updates are not per variable (there are no technical variable names in the blob) but instead by element path. So when we get an update for, say, Controller1.OutdoorTemp we must be able to translate that to the correct path/paths. |
| messageStream | ReplaySubject | An observable stream which can be "updated" and checked for observers. |

##### View-/VariableTree

We'll probably need some sort of "mapper" between the actual (filtered) view and the "path'ed" updates.