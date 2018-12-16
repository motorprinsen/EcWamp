using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace WampClient
{ 
        public class AreaTree : IEnumerable<AclTreeNode>
        {
            public AclTreeNode RootNode { get; set; }

            public AreaTree Clone()
            {
                AreaTree clone = new AreaTree();
                clone.RootNode = CloneNode(null, RootNode);
                return clone;
            }

            private AclTreeNode CloneNode(AclTreeNode parent, AclTreeNode nodeToClone)
            {
                AclTreeNode clone = new AclTreeNode(parent, nodeToClone.Name);
                clone.ShowInScada = nodeToClone.ShowInScada;
                clone.UseInFullTitle = nodeToClone.UseInFullTitle;
                clone.Title = nodeToClone.Title;
                foreach (AclTreeNode childNode in nodeToClone.ChildNodes)
                {
                    CloneNode(clone, childNode);
                }
                return clone;
            }

            private Dictionary<int, AclTreeNode> _findIdCache;
            private Dictionary<string, AclTreeNode> _findCache;
            public AclTreeNode Find(string areaName)
            {
                if (_findIdCache == null)
                {
                    _findIdCache = new Dictionary<int, AclTreeNode>();
                }
                if (_findCache == null)
                {
                    _findCache = new Dictionary<string, AclTreeNode>(StringComparer.OrdinalIgnoreCase);
                }
                if (string.IsNullOrEmpty(areaName)) return null;
                AclTreeNode cacheItem = null;
                if (_findCache.TryGetValue(areaName, out cacheItem))
                    return cacheItem;
                cacheItem = RootNode.Cast<AclTreeNode>().Where(treeNode => treeNode.Equals(areaName)).FirstOrDefault();
                _findCache[areaName] = cacheItem;
                if (cacheItem != null)
                    _findIdCache[cacheItem.Id] = cacheItem;
                return cacheItem;
            }

            public AclTreeNode Find(AclTreeNode node)
            {
                return Find(node.Name);
            }

            public AclTreeNode FindById(int id)
            {
                AclTreeNode cacheItem = null;
                if (_findIdCache.TryGetValue(id, out cacheItem))
                    return cacheItem;
                cacheItem = RootNode.Cast<AclTreeNode>().Where(treeNode => treeNode.Id == id).FirstOrDefault();
                if (cacheItem != null)
                    _findCache[cacheItem.Name] = cacheItem;
                _findIdCache[id] = cacheItem;

                return cacheItem;
            }

            internal bool SetAccess(string area, string access, AccessItem accessItem)
            {
                AclTreeNode aclTreeNode = Find(area);
                if (aclTreeNode == null) return false;
                bool accessSet = false;
                aclTreeNode.UnSetAccess(accessItem);
                if (access != "Inherit" && (area == "Root" || area == "RootArea" || (AccessLevels.HasAccess(aclTreeNode.Access, access) && accessItem is UserGroup) || accessItem is User))
                {
                    aclTreeNode.SetAccess(access, accessItem);
                    accessSet = true;
                }
                return accessSet;
            }

            internal string GetAccess(string area)
            {
                var aclNode = Find(area);
                return aclNode == null ? "" : aclNode.Access;
            }

            private void GetNodeList(AclTreeNode node, Queue<AclTreeNode> outPut)
            {
                outPut.Enqueue(node);
                foreach (AclTreeNode aclTreeNode in node.ChildNodes)
                {
                    GetNodeList(aclTreeNode, outPut);
                }
            }

            public IEnumerator<AclTreeNode> GetEnumerator()
            {
                Queue<AclTreeNode> output = new Queue<AclTreeNode>();

                GetNodeList(RootNode, output);

                while (output.Count > 0)
                    yield return output.Dequeue();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        public class AccessEntry
        {
            public string Access { get; private set; }

            public AccessItem Reason { get; private set; }

            public AccessEntry(string access, AccessItem reason)
            {
                Access = access;
                Reason = reason;
            }
        }
        public class AddInUser
        {
            public string Account;
            public string UserName;
            public string Password;
            public string Id;
            public string Location;
        }
        public class DiagnosticDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            protected object tag;
            protected string name = "unknown";

            /// <summary>
            /// Gets/sets an object that you can associate with the dictionary.
            /// </summary>
            /// De
            public object Tag
            {
                get { return tag; }
                set { tag = value; }
            }

            /// <summary>
            /// The dictionary name. The default is "unknown". 
            /// Used to enhance the KeyNotFoundException.
            /// </summary>

            public string Name
            {
                get { return name; }
                set { name = value; }
            }

            /// <summary>
            /// Parameterless constructor.
            /// </summary>
            public DiagnosticDictionary()
            {

            }

            public DiagnosticDictionary(IEqualityComparer<TKey> comp) : base(comp)
            {
            }

            /// <summary>
            /// Constructor that takes a name.
            /// </summary>
            public DiagnosticDictionary(string name)
            {
                this.name = name;
            }

            /// <summary>
            /// Constructor that takes a name.
            /// </summary>
            public DiagnosticDictionary(string name, IEqualityComparer<TKey> comp) : base(comp)
            {
                this.name = name;
            }
            /// <summary>
            /// Indexer that produces a more useful KeyNotFoundException.
            /// </summary>
            public new TValue this[TKey key]
            {
                get
                {
                    try
                    {
                        return base[key];
                    }
                    catch (KeyNotFoundException)
                    {

                        throw new KeyNotFoundException("The key '" + key.ToString() +
                           "' was not found in the dictionary '" + name + "'. in method: " + System.Reflection.MethodBase.GetCurrentMethod().Name);
                    }
                }

                set { base[key] = value; }
            }
            public void Add(Dictionary<TKey, TValue> indict)
            {
                foreach (KeyValuePair<TKey, TValue> kvp in indict)
                {
                    Add(kvp.Key, kvp.Value);
                }
            }
        }
        public class AreaAcess : IEnumerable
        {
            public Dictionary<string, string> Areas { get; set; }

            public AreaAcess()
            {
                Areas = new DiagnosticDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            public bool Empty
            {
                get
                {
                    return Areas.Count == 0;
                }
            }

            public void UpdateAccess(string name, string value)
            {
                string currentUserAccess;
                if (Areas.TryGetValue(name, out currentUserAccess))
                {
                    if (AccessLevels.HasAccess(value, currentUserAccess))
                    {
                        Areas[name] = value;
                    }
                }
                else
                    Areas[name] = value;
            }

            public IEnumerator GetEnumerator()
            {
                return Areas.GetEnumerator();
            }
        }
        public class UserGroup : AccessItem
        {
            public UserGroup(string name) : base(name)
            {
                Members = new HashSet<AccessItem>();
            }

            public HashSet<AccessItem> Members { get; private set; }

        }
        public class AccessItem
        {
            public Dictionary<string, string> Propertys;

            public List<AccessItem> AccessItems = new List<AccessItem>();

            public bool VisibleInUI = true;

            public AccessLevels.Levels SiteAccess;

            //[DataMember]
            //public AreaTree AclRootNode { get; set; }

            public AccessItem(string name)
            {
                Areas = new AreaAcess();
                Propertys = new DiagnosticDictionary<string, string>("Propertys of user " + name);
                this["Name"] = name;
                SiteAccess = AccessLevels.Levels.Guest;
            }

            public AreaAcess Areas { get; private set; }

            public string this[string key]
            {
                get { return Propertys[key]; }
                set { Propertys[key] = value; }
            }

            public string Name
            {
                get
                {
                    return this["Name"]; // as in users.exoxml
                }
                set { Propertys["Name"] = value; }
            }

            private AccessItem _administratedBy;
            public AccessItem AdministratedBy
            {
                get { return _administratedBy; }
                set
                {
                    if (value == null) return;
                    Propertys["AdministratedBy"] = value.Name;
                    _administratedBy = value;
                }
            }

            public string Password
            {
                get { return HasProperty("Password") ? Propertys["Password"] : ""; }
                set { Propertys["Password"] = value; }
            }
            public string UserDescription
            {
                get { return HasProperty("UserDescription") ? Propertys["UserDescription"] : ""; }
                set { Propertys["UserDescription"] = value; }
            }
            public string Title
            {
                get
                {
                    string title = HasProperty("Title") ? Propertys["Title"] : "";
                    return string.IsNullOrWhiteSpace(title) ? Name : title;
                }
                set { Propertys["Title"] = value; }
            }
            public string DefaultScale
            {
                get { return HasProperty("DefaultScale") ? Propertys["DefaultScale"] : ""; }
                set { Propertys["DefaultScale"] = value; }
            }

            public TimeSpan TimeOut
            {
                get
                {

                    if (HasProperty("TimeOut") && Propertys["TimeOut"].Trim().Equals("None"))
                    {
                        return new TimeSpan(9000, 0, 0, 0);
                    }

                    return HasProperty("TimeOut") ? TimeSpan.Parse(Propertys["TimeOut"]) : new TimeSpan(1, 0, 0);
                }
                set { Propertys["TimeOut"] = value.ToString(); }
            }

            public string Access
            {
                get { return HasProperty("AccessLevel") ? Propertys["AccessLevel"] : AccessLevels.Default; }
                set
                {
                    Propertys["AccessLevel"] = value;

                }

            }

            public string Comment
            {
                get { return HasProperty("Comment") ? Propertys["Comment"] : ""; }
                set { Propertys["Comment"] = value; }
            }

            public string Telephone
            {
                get { return HasProperty("Telephone") ? Propertys["Telephone"] : ""; }
                set { Propertys["Telephone"] = value; }
            }

            public string Company
            {
                get { return HasProperty("Company") ? Propertys["Company"] : ""; }
                set { Propertys["Company"] = value; }
            }

            public string Email
            {
                get { return HasProperty("EMail") ? Propertys["EMail"] : ""; }
                set { Propertys["EMail"] = value; }
            }

            public bool SendAlarms
            {
                get { return HasProperty("SendAlarms") && Propertys["SendAlarms"] == "Yes"; }
                set { Propertys["SendAlarms"] = value ? "Yes" : "No"; }
            }

            public string SendPriorities
            {
                get { return HasProperty("SendPriorities") ? Propertys["SendPriorities"] : ""; }
                set { Propertys["SendPriorities"] = value; }
            }
            public bool SendActivates
            {
                get { return HasProperty("SendActivates") && Propertys["SendActivates"] == "Yes"; }
                set { Propertys["SendActivates"] = value ? "Yes" : "No"; }
            }
            public bool SendSwitchOn
            {
                get { return HasProperty("SendSwitchOn") && Propertys["SendSwitchOn"] == "Yes"; }
                set { Propertys["SendSwitchOn"] = value ? "Yes" : "No"; }
            }
            public bool SendSwitchOff
            {
                get { return HasProperty("SendSwitchOff") && Propertys["SendSwitchOff"] == "Yes"; }
                set { Propertys["SendSwitchOff"] = value ? "Yes" : "No"; }
            }
            public bool SendAck
            {
                get { return HasProperty("SendAck") && Propertys["SendAck"] == "Yes"; }
                set { Propertys["SendAck"] = value ? "Yes" : "No"; }
            }
            public bool SendBlock
            {
                get { return HasProperty("SendBlock") && Propertys["SendBlock"] == "Yes"; }
                set { Propertys["SendBlock"] = value ? "Yes" : "No"; }
            }
            public bool SendUnblock
            {
                get { return HasProperty("SendUnblock") && Propertys["SendUnblock"] == "Yes"; }
                set { Propertys["SendUnblock"] = value ? "Yes" : "No"; }
            }
            public string State
            {
                get { return HasProperty("State") ? Propertys["State"] : "Normal"; }
                set { Propertys["State"] = value; }
            }

            public DateTime PasswordChanged
            {
                get
                {
                    if (HasProperty("PasswordChanged"))
                    {
                        var value = Propertys["PasswordChanged"];
                        DateTime outTime;
                        if (DateTime.TryParse(value, out outTime))
                            return outTime;
                    }
                    return DateTime.MinValue;
                }
                set { Propertys["PasswordChanged"] = value.ToString("yyyy-MM-dd"); }
            }

            public void AddAccess(string path, string access)
            {
                Areas.UpdateAccess(path, access);
            }

            public bool HasProperty(string propName)
            {
                return Propertys.ContainsKey(propName);
            }

            public override string ToString()
            {
                return string.Format("Name: {0} Access: {1}", Name, Access);
            }

            public override bool Equals(object obj)
            {
                AccessItem accessItem = obj as AccessItem;

                return accessItem == null ? false : Equals(accessItem);

            }

            public bool Equals(AccessItem other)
            {
                return Name.Equals(other.Name);
            }

        }
        public class User : AccessItem
        {
            public string EXO4WebUserName { get { return Propertys.ContainsKey("EXO4WebUserName") ? Propertys["EXO4WebUserName"].ToString() : ""; } set { Propertys["EXO4WebUserName"] = value.ToString(); } }

            private string _layout = "SiteLayout";

            public string Layout { get { return _layout; } set { _layout = value; } }

            public string Theme { get; set; }

            public int LCID { get; set; }

            public string Account { get; set; }

            public AddInUser AddInUser { get; set; }

            public Dictionary<string, string> ClientData = new Dictionary<string, string>();

            public User(string name) : base(name)
            {

            }
        }
        public static class AccessLevels
        {
            public enum Levels { None, Browse, Guest, Operator, Service, Admin, SysAdmin, System }
            private static readonly List<string> levels =
                new List<string>(new[] { "None", "Browse", "Guest", "Operator", "Service", "Admin", "SysAdmin", "System" });

            public static string Default
            {
                get
                {
                    return "Guest";
                }
            }

            public static bool HasAccess(string lowestValidAccess, string currentUserAccess)
            {
                return levels.IndexOf(lowestValidAccess) <= levels.IndexOf(currentUserAccess);
            }
            public static bool HasAccess(Levels lowestValidAccess, string currentUserAccess)
            {
                return levels.IndexOf(lowestValidAccess.ToString()) <= levels.IndexOf(currentUserAccess);
            }
            public static bool HasAccess(string lowestValidAccess, Levels currentUserAccess)
            {
                return levels.IndexOf(lowestValidAccess) <= levels.IndexOf(currentUserAccess.ToString());
            }
            public static bool HasAccess(Levels lowestValidAccess, Levels currentUserAccess)
            {
                return lowestValidAccess <= currentUserAccess;
            }
        }
        public class Holder
        {
            public string Name;
            public string Location;
            public override string ToString()
            {
                return Name + " " + Location;
            }
        }
        public interface ITreeNode
        {
            bool IsBusy { get; }

            bool Zombie { get; set; }


            List<ITreeNode> ChildNodes { get; }


            Guid ItemId { get; }


            ITreeNode Parent { get; set; }


            string Name { get; set; }

            System.Collections.IEnumerator GetEnumerator();

            void MakeBusy(Holder holder);
            void MakeUnBusy(Holder holder);
        }
        public abstract class TreeNode : ITreeNode, IEnumerable
        {

            public Guid ItemId
            {
                get;
                private set;
            }

            public ITreeNode Parent
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }
            public List<ITreeNode> ChildNodes
            {
                get;
                private set;
            }

            private List<Holder> _isBusy = new List<Holder>();

            public bool IsBusy { get { return _isBusy.Count > 0; } }

            public void MakeBusy(Holder holder)
            {
                //System.Threading.Interlocked.Increment(ref _isBusy);
                _isBusy.Add(holder);
                //if ((Logger.LogTypeIsOn(LogTypes.debug)))
                //Logger.log("TreeNode:", string.Format("{2} setting {0} busy. Current holders:{1}", Name, _isBusy.Aggregate("", (current, entry) => current + "," + entry.Name), holder.Name), LogTypes.debug);
                if (Parent != null)
                    Parent.MakeBusy(holder);
            }

            public void MakeUnBusy(Holder holder)
            {
                //System.Threading.Interlocked.Decrement(ref _isBusy);
                _isBusy.Remove(holder);
                // if ((Logger.LogTypeIsOn(LogTypes.debug)))
                //Logger.log("TreeNode:", string.Format("{2} setting {0} unbusy. Current holders:{1}", Name, _isBusy.Aggregate("", (current, entry) => current + "," + entry.Name), holder.Name), LogTypes.debug);
                if (Parent != null)
                    Parent.MakeUnBusy(holder);
            }


            private bool _zombie;
            public bool Zombie
            {
                get { return _zombie; }
                set
                {
                    // if ((Logger.LogTypeIsOn(LogTypes.debug)))
                    //Logger.log(ToString(), "Setting zombie from " + _zombie + " to:" + value, LogTypes.debug);
                    _zombie = value;
                }
            }

            public System.Collections.IEnumerator GetEnumerator()
            {
                Queue<ITreeNode> childNodes = new Queue<ITreeNode>();
                childNodes.Enqueue(this);
                while (childNodes.Count > 0)
                {

                    TreeNode node = (TreeNode)childNodes.Dequeue();
                    foreach (TreeNode c in node.ChildNodes)
                        childNodes.Enqueue(c);
                    yield return node;
                }
            }

            public TreeNode(ITreeNode parent, string Name)
            {
                this.ChildNodes = new List<ITreeNode>();
                this.Parent = parent;
                this.Name = Name;
                ItemId = Guid.NewGuid();
                if (parent != null)
                {
                    parent.ChildNodes.Add(this);
                }
            }
        }
        public class AclTreeNode : TreeNode
        {
            //special for treeListCOntrol
            public bool NoneAccess { get { return AccessLevels.HasAccess(AccessLevels.Levels.None, Access); } }
            public bool BrowseAccess { get { return AccessLevels.HasAccess(AccessLevels.Levels.Browse, Access); } }
            public bool GuestAccess { get { return AccessLevels.HasAccess(AccessLevels.Levels.Guest, Access); } }
            public bool OperatorAccess { get { return AccessLevels.HasAccess(AccessLevels.Levels.Operator, Access); } }
            public bool SystemAccess { get { return AccessLevels.HasAccess(AccessLevels.Levels.System, Access); } }
            public bool SysAdminAccess { get { return AccessLevels.HasAccess(AccessLevels.Levels.SysAdmin, Access); } }
            public bool AdminAccess { get { return AccessLevels.HasAccess(AccessLevels.Levels.Admin, Access); } }

            private static long _nextId = 0;

            private int _id = 0;

            public int Id
            {
                get
                {
                    if (_id == 0)
                    {
                        _id = (int)System.Threading.Interlocked.Increment(ref _nextId);
                    }
                    return _id;
                }
            }

            public int ParentId
            {
                get
                {
                    if (Parent == null) return 0;
                    ITreeNode p = Parent;
                    while (p != null)
                    {
                        if (!((AclTreeNode)p).ShowInScada)
                            p = p.Parent;
                        else
                        {
                            break;
                        }
                    }
                    return p != null ? ((AclTreeNode)p).Id : 0;
                }
            }


            private List<AccessEntry> _access = new List<AccessEntry>();

            public ReadOnlyCollection<AccessEntry> AccessList { get { return _access.AsReadOnly(); } }



            private string _title;


            public string DefaultController;

            public string Title
            {
                get { return string.IsNullOrWhiteSpace(_title) ? Name : _title; }
                set { _title = value; }
            }

            public string Access
            {
                get
                {
                    string access = null;
                    AccessEntry firstOrDefault = _access.FirstOrDefault(ai => ai.Reason is User);
                    if (firstOrDefault != null)
                    {
                        return firstOrDefault.Access;
                    }
                    foreach (AccessEntry accessEntry in _access)
                    {
                        if (access == null)
                        {
                            access = accessEntry.Access;
                        }
                        else
                        {
                            if (AccessLevels.HasAccess(access, accessEntry.Access))
                                access = accessEntry.Access;
                        }
                    }

                    return access ?? (Parent != null ? ((AclTreeNode)Parent).Access : AccessLevels.Levels.None.ToString());
                }
            }

            public bool InheritedAccess { get { return _access.Count == 0; } }
            public bool ExplicitAccessBelow
            {
                get
                {
                    bool childHasExplicitAccess = ChildNodes.Any(n => !((AclTreeNode)n).InheritedAccess);
                    if (childHasExplicitAccess)
                        return true;
                    foreach (AclTreeNode childNode in ChildNodes)
                    {
                        if (childNode.ExplicitAccessBelow)
                            return true;
                    }
                    return false;
                }
            }
            public void SetAccess(string access, AccessItem accessItem)
            {
                _access.RemoveAll(m => m.Reason == accessItem);
                _access.Add(new AccessEntry(access, accessItem));
            }

            public void UnSetAccess(AccessItem accessItem)
            {
                _access.RemoveAll(m => m.Reason == accessItem);
            }


            public AccessItem ReasonWhyAccess
            {
                get
                {

                    AccessEntry entry = null;

                    AccessEntry firstOrDefault = _access.FirstOrDefault(ai => ai.Reason is User);
                    if (firstOrDefault != null)
                    {
                        return firstOrDefault.Reason;
                    }

                    foreach (AccessEntry accessEntry in _access)
                    {
                        if (entry == null)
                        {
                            entry = accessEntry;
                        }
                        else
                        {
                            if (AccessLevels.HasAccess(entry.Access, accessEntry.Access))
                                entry = accessEntry;
                        }
                    }
                    if (entry == null)
                    {
                        return ((AclTreeNode)Parent).ReasonWhyAccess;
                    }
                    else
                    {
                        return entry.Reason;
                    }
                }

            }


            public bool ShowInScada { get; set; }


            public bool UseInFullTitle { get; set; }


            public bool IsAddInNode { get; set; }


            public AclTreeNode(ITreeNode parent, string name)
                : base(parent, name)
            {
            }

            public override bool Equals(object obj)
            {
                AclTreeNode otherNode = (obj as AclTreeNode);

                string name = (obj as string);

                if (otherNode != null) return Name.ToLower().Equals(otherNode.Name.ToLower());

                if (name != null) return Name.ToLower().Equals(name.ToLower());

                return false;
            }


            public override string ToString()
            {
                return Name + " Access: " + Access;
            }

            internal void ClearAccess()
            {
                _access.Clear();
            }
        }
        public class AccessControl
        {
            public bool UseCache = false;
            public static string EmergencyUserName = "Emergency";

            public Dictionary<string, Dictionary<string, AclTreeNode>> ExplicitAccess =
                new Dictionary<string, Dictionary<string, AclTreeNode>>();
            public Dictionary<string, UserGroup> Groups;

            public Dictionary<string, User> Users;
            public Dictionary<string, string> GlobalSettings = new Dictionary<string, string>();

            public Dictionary<string, User> AddInUsers = new Dictionary<string, User>();
            public bool AllowDeleteUser { get; set; }
            public bool IsChanged { get; set; }


            private AccessItem FindAccessItem(string accessItemName)
            {
                User u;
                UserGroup g;
                if (Users.TryGetValue(accessItemName, out u))
                    return u;
                if (AddInUsers.TryGetValue(accessItemName, out u))
                    return u;
                if (Groups.TryGetValue(accessItemName, out g))
                    return g;


                return null;
            }


            private byte[] _compressedAccessControl;


            //public  Dictionary<AccessItem, AreaTree> AreaTrees;

            //private Dictionary<AccessItem, AreaTree> _newAreaTrees;

            public string Comment;

            private Dictionary<string, string> FirstAreaForUser = new DiagnosticDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public AccessControl()
            {
                UseCache = true;
            }

            public AreaTree AreaTreeStructure;

            /// <summary>
            /// Rewrites all access in all trees. Call this when updating ANY group access or group membership. OK?
            /// </summary>
            public void UpdateGroupAccess()
            {
                ExplicitAccess = new Dictionary<string, Dictionary<string, AclTreeNode>>();

                Stopwatch startNew = Stopwatch.StartNew();

                var acc = Users.Values.Cast<AccessItem>().ToList();
                acc.AddRange(Groups.Values);

                foreach (AccessItem accessItem in acc)
                {
                    if (!accessItem.Propertys.ContainsKey("AdministratedBy"))
                        continue;

                    string s = accessItem.Propertys["AdministratedBy"];

                    accessItem.AdministratedBy = FindAccessItem(s);
                }

                //Compress();
                //DeCompress();
                startNew.Stop();
                //Logger.log("AccessControl.Initialize", "Building accesscontrol structure took ~" + startNew.ElapsedMilliseconds, LogTypes.timing);
            }

            public List<AclTreeNode> GetChildrenFromArea(string username, string area)
            {
                if (!AccessLevels.HasAccess("Browse", GetAccessInArea(username, area).Item1))
                    return new List<AclTreeNode>();

                ResolveAccess(username, 0, null);
                AclTreeNode node = AreaTreeStructure.Find(area);

                List<AclTreeNode> l = new List<AclTreeNode>();
                List<AclTreeNode> aclTreeNodes = node.ChildNodes.Cast<AclTreeNode>().ToList();
                foreach (AclTreeNode aclTreeNode in aclTreeNodes)
                {
                    if (aclTreeNode.ShowInScada && AccessLevels.HasAccess("Browse", GetAccessInArea(username, aclTreeNode.Name).Item1) && HasAccessToNodeOrBeneath(username, aclTreeNode))
                        l.Add(aclTreeNode);

                    if (!aclTreeNode.ShowInScada)
                        l.AddRange(GetChildrenFromArea(username, aclTreeNode.Name));

                }
                return l;
            }

            private bool HasAccessToNodeOrBeneath(string username, AclTreeNode treeNode)
            {
                if (treeNode.ChildNodes.Count == 0)
                    return true;
                foreach (AclTreeNode childNode in treeNode)
                {
                    if (AccessLevels.HasAccess("Guest", GetAccessInArea(username, childNode.Name).Item1))
                        return true;
                }
                return false;
            }

            private Dictionary<string, Dictionary<string, Tuple<string, AccessItem, AclTreeNode>>> accessCache;
            public Tuple<string, AccessItem, AclTreeNode> GetAccessInArea(string accessItemName, string area)
            {
                HashSet<AccessItem> collectedItems = new HashSet<AccessItem>();
                return GetAccessInArea(accessItemName, area, collectedItems);
            }

            public Tuple<string, AccessItem, AclTreeNode> GetAccessInArea(string accessItemName, string area, HashSet<AccessItem> collectedItems)
            {
                if (UseCache)
                {
                    if (accessCache == null)
                        accessCache =
                            new Dictionary<string, Dictionary<string, Tuple<string, AccessItem, AclTreeNode>>>();
                    Dictionary<string, Tuple<string, AccessItem, AclTreeNode>> usercache;
                    if (accessCache.TryGetValue(accessItemName, out usercache))
                    {
                        Tuple<string, AccessItem, AclTreeNode> entry;
                        if (usercache.TryGetValue(area, out entry))
                            return entry;
                    }
                }
                ResolveAccess(accessItemName, 0, collectedItems);
                AclTreeNode n = AreaTreeStructure.Find(area);
                if (n == null) return new Tuple<string, AccessItem, AclTreeNode>(AccessLevels.Levels.None.ToString(), null, null);
                //n är noden vi hämtar access för

                Dictionary<string, AclTreeNode> explicitAccess = null;
                //explicit access är dictionary av areor och explicit access för den arean.
                ExplicitAccess.TryGetValue(accessItemName, out explicitAccess);
                Tuple<string, AccessItem, AclTreeNode> cacheEntry = null;
                if (explicitAccess != null)
                {
                    while (n != null && n.Name != "Root")
                    {
                        AclTreeNode explicitAccessNode = null;
                        //om vi har explicit access för nodens namn, hämta den accessen.
                        if (explicitAccess.TryGetValue(n.Name, out explicitAccessNode))
                        {
                            cacheEntry = new Tuple<string, AccessItem, AclTreeNode>(explicitAccessNode.Access,
                                                                       explicitAccessNode.ReasonWhyAccess, explicitAccessNode);
                            break;
                        }
                        //annars, kolla om vi har explicit access som vi skall ärva ner till denna noden.
                        n = n.Parent as AclTreeNode;
                    }
                }
                if (cacheEntry == null)
                {
                    AccessItem i = null;
                    User item;
                    UserGroup g;
                    if (Users.TryGetValue(accessItemName, out item))
                        i = item;
                    else if (Groups.TryGetValue(accessItemName, out g))
                        i = g;
                    else if (AddInUsers.TryGetValue(accessItemName, out item))
                        i = item;

                    string access = i != null ? i.Access : "None";

                    cacheEntry = new Tuple<string, AccessItem, AclTreeNode>(access, item, null);
                }
                if (UseCache)
                {
                    if (!accessCache.ContainsKey(accessItemName))
                        accessCache[accessItemName] = new Dictionary<string, Tuple<string, AccessItem, AclTreeNode>>();
                    accessCache[accessItemName][area] = cacheEntry;
                }
                return cacheEntry;
            }


            private Dictionary<string, Dictionary<string, string>> fullTitleCache;
            public string GetFullTitle(string username, string area)
            {
                if (UseCache)
                {
                    if (fullTitleCache == null)
                        fullTitleCache =
                            new Dictionary<string, Dictionary<string, string>>();
                    Dictionary<string, string> usercache;
                    if (fullTitleCache.TryGetValue(username, out usercache))
                    {
                        string title;
                        if (usercache.TryGetValue(area, out title))
                            return title;
                    }
                }
                //find accesscontrol node
                ResolveAccess(username, 0, null);
                AclTreeNode node = AreaTreeStructure.Find(area);
                AclTreeNode areaNode = node;
                node = node.Parent as AclTreeNode;

                if (node == null) return null;
                Dictionary<string, AclTreeNode> explicitAccess = null;
                ExplicitAccess.TryGetValue(username, out explicitAccess);
                //find accessList to node. 
                string fulltitle = "";
                string parenttitle = "";
                while (node != null && node.Name != "Root")
                {
                    var access = GetAccessInArea(username, node.Name);
                    bool useInFullTitle = node.UseInFullTitle;

                    if (node.ShowInScada && useInFullTitle &&
                        AccessLevels.HasAccess(AccessLevels.Levels.Browse, access.Item1))
                    {
                        if (string.IsNullOrEmpty(parenttitle))
                            parenttitle = node.Title;
                        else
                            parenttitle = node.Title + " - " + parenttitle;
                    }

                    node = node.Parent as AclTreeNode;
                }

                if (string.IsNullOrEmpty(parenttitle))
                    fulltitle = areaNode.Title;
                else
                {
                    fulltitle = parenttitle + " - " + areaNode.Title;
                }

                if (UseCache)
                {
                    if (!fullTitleCache.ContainsKey(username))
                        fullTitleCache[username] = new Dictionary<string, string>();
                    fullTitleCache[username][area] = fulltitle;
                }
                return fulltitle;
            }


            private Dictionary<string, Dictionary<string, string>> fullNameCache;
            public string GetFullName(string username, string area)
            {
                if (UseCache)
                {
                    if (fullNameCache == null)
                        fullNameCache =
                            new Dictionary<string, Dictionary<string, string>>();
                    Dictionary<string, string> userCache;
                    if (fullNameCache.TryGetValue(username, out userCache))
                    {
                        string cachedName;
                        if (userCache.TryGetValue(area, out cachedName))
                            return cachedName;
                    }
                }
                ResolveAccess(username, 0, null);
                AclTreeNode node = AreaTreeStructure.Find(area);
                AclTreeNode areaNode = node;
                node = node.Parent as AclTreeNode;

                if (node == null) return null;
                Dictionary<string, AclTreeNode> explicitAccess = null;
                ExplicitAccess.TryGetValue(username, out explicitAccess);
                //find accessList to node. 
                string parentPath = "";
                string fullName = "";
                while (node != null && node.Name != "Root")
                {
                    var access = GetAccessInArea(username, node.Name);
                    if (node.ShowInScada && AccessLevels.HasAccess(AccessLevels.Levels.Browse, access.Item1))
                    {
                        if (string.IsNullOrEmpty(parentPath))
                            parentPath = node.Name;
                        else
                        {
                            parentPath = node.Name + "." + parentPath;
                        }
                    }
                    node = node.Parent as AclTreeNode;
                }
                if (string.IsNullOrEmpty(parentPath))
                    fullName = areaNode.Name;
                else
                {
                    fullName = parentPath + "." + areaNode.Name;
                }
                if (UseCache)
                {
                    if (!fullNameCache.ContainsKey(username))
                        fullNameCache[username] = new Dictionary<string, string>();
                    fullNameCache[username][area] = fullName;
                }
                return fullName;
            }

            private Dictionary<string, Dictionary<string, bool>> explicitAccessBelowCache;

            public bool GetExplicitAccessBelow(string username, string area)
            {
                Dictionary<string, bool> exAcc = null;
                if (UseCache)
                {

                    if (explicitAccessBelowCache == null)
                        explicitAccessBelowCache = new Dictionary<string, Dictionary<string, bool>>();

                    if (explicitAccessBelowCache.TryGetValue(username, out exAcc))
                    {
                        bool exp;
                        if (exAcc.TryGetValue(area, out exp))
                            return exp;
                    }
                }
                ResolveAccess(username, 0, null);

                Dictionary<string, AclTreeNode> explicitAccess;
                if (!ExplicitAccess.TryGetValue(username, out explicitAccess))
                    return false;
                AclTreeNode aclTreeNode = AreaTreeStructure.Find(area);
                bool ret = false;
                foreach (var treeNode in explicitAccess)
                {

                    string name = treeNode.Value.Name;
                    AclTreeNode node = AreaTreeStructure.Find(name);
                    while (node != null && node != aclTreeNode && !aclTreeNode.ChildNodes.Contains(node))
                    {
                        node = node.Parent as AclTreeNode;
                    }

                    ret = aclTreeNode.ChildNodes.Contains(node);
                    if (ret)
                        break;
                }
                if (UseCache)
                {
                    if (exAcc == null)
                        exAcc = explicitAccessBelowCache[username] = new Dictionary<string, bool>();
                    exAcc[area] = ret;
                }

                return ret;
            }

            public bool SetUserAccess(string area, string access, string userName)
            {
                User u;
                if (!Users.TryGetValue(userName, out u))
                    AddInUsers.TryGetValue(userName, out u);

                return SetAccess(area, access, u);
            }
            public bool SetGroupAccess(string area, string access, string userName)
            {
                return SetAccess(area, access, Groups.Values.SingleOrDefault(g => g.Name.Equals(userName)));
            }
            private bool SetAccess(string area, string access, AccessItem accessItem)
            {
                //find accessitem

                AccessItem i = accessItem;
                if (i == null)
                    return false;

                bool accessSet = false;

                if (area == null)
                {
                    i.Access = access;
                }
                else if (access == "Inherit")
                {
                    if (i.Areas.Areas.ContainsKey(area))
                        i.Areas.Areas.Remove(area);
                    accessSet = true;
                }
                else
                {
                    i.Areas.Areas[area] = access;
                    accessSet = true;
                }

                return accessSet;
            }

            public void ClearCache()
            {
                accessCache = new Dictionary<string, Dictionary<string, Tuple<string, AccessItem, AclTreeNode>>>();
                fullTitleCache = new Dictionary<string, Dictionary<string, string>>();
                fullNameCache = new Dictionary<string, Dictionary<string, string>>();
                explicitAccessBelowCache = new Dictionary<string, Dictionary<string, bool>>();
                isAdminSomeWhereCache = new Dictionary<string, bool?>();
                accessResolved = new HashSet<string>();
            }

            private Dictionary<string, bool?> isAdminSomeWhereCache;
            public bool IsAdminSomewhere(AccessItem accessItem)
            {
                bool? isAdmin = null;
                if (UseCache)
                {
                    if (isAdminSomeWhereCache == null) isAdminSomeWhereCache = new Dictionary<string, bool?>();

                    if (isAdminSomeWhereCache.TryGetValue(accessItem.Name, out isAdmin))
                        return isAdmin.Value;
                }
                ResolveAccess(accessItem.Name, 0, null);
                var accessItemName = accessItem.Name;
                Dictionary<string, AclTreeNode> explicitAccess = null;
                ExplicitAccess.TryGetValue(accessItemName, out explicitAccess);

                if (explicitAccess != null)
                {
                    foreach (var aclTreeNode in explicitAccess)
                    {
                        if (aclTreeNode.Key.Equals("RootArea") && aclTreeNode.Value.Access.Equals("None"))
                            isAdmin = false;

                        if (aclTreeNode.Value.AdminAccess)
                        {
                            isAdmin = true;

                        }
                    }
                }

                if (isAdmin == null)
                {
                    Tuple<string, AccessItem, AclTreeNode> accessInArea = GetAccessInArea(accessItem.Name, "RootArea");
                    if (AccessLevels.HasAccess(AccessLevels.Levels.Admin, accessInArea.Item1))
                        isAdmin = true;
                    else
                    {
                        isAdmin = false;
                    }
                }

                if (UseCache)
                    isAdminSomeWhereCache[accessItem.Name] = isAdmin;
                return isAdmin.Value;
            }

            private Dictionary<string, bool?> hasAccessSomeWhereCache = new DiagnosticDictionary<string, bool?>();

            public bool HasOperatorAccessSomeWhere(AccessItem accessItem)
            {
                return HasAccessSomewhere(accessItem, AccessLevels.Levels.Operator);
            }
            private bool HasAccessSomewhere(AccessItem accessItem, AccessLevels.Levels acesslevel)
            {
                bool? hasAccess = null;
                if (UseCache)
                {
                    if (hasAccessSomeWhereCache == null) hasAccessSomeWhereCache = new Dictionary<string, bool?>();

                    if (hasAccessSomeWhereCache.TryGetValue(accessItem.Name, out hasAccess))
                        return hasAccess.Value;
                }
                ResolveAccess(accessItem.Name, 0, null);
                var accessItemName = accessItem.Name;
                Dictionary<string, AclTreeNode> explicitAccess = null;
                ExplicitAccess.TryGetValue(accessItemName, out explicitAccess);

                if (explicitAccess != null)
                {
                    foreach (var aclTreeNode in explicitAccess)
                    {
                        if (aclTreeNode.Key.Equals("RootArea") && aclTreeNode.Value.Access.Equals("None"))
                            hasAccess = false;

                        if (AccessLevels.HasAccess(acesslevel, aclTreeNode.Value.Access))
                        {
                            hasAccess = true;
                        }
                    }
                }

                if (hasAccess == null)
                {
                    Tuple<string, AccessItem, AclTreeNode> accessInArea = GetAccessInArea(accessItem.Name, "RootArea");
                    if (AccessLevels.HasAccess(acesslevel, accessInArea.Item1))
                        hasAccess = true;
                    else
                    {
                        hasAccess = false;
                    }
                }

                if (UseCache)
                    hasAccessSomeWhereCache[accessItem.Name] = hasAccess;
                return hasAccess.Value;
            }

            public AccessLevels.Levels GetAccessForAccessItem(AccessItem item)
            {
                //find all groups the accessitem is member of. 
                List<AccessItem> list = new List<AccessItem>();

                Dictionary<string, AclTreeNode> aclTreeNodes;
                GetGroupsForGroups(item, ref list);


                //ResolveAccess(accessItem);

                //get highest access for the access attribute
                AccessLevels.Levels level;
                Enum.TryParse(item.Access, out level);
                foreach (AccessItem i in list)
                {
                    if (AccessLevels.HasAccess(level, item.Access))
                        Enum.TryParse(i.Access, out level);
                }
                return level;
            }

            private HashSet<string> accessResolved;
            public void ResolveAccess(AccessItem item, int deepLevel, HashSet<AccessItem> collectedItems)
            {
                if (item == null)
                    return;
                if (collectedItems == null)
                    collectedItems = new HashSet<AccessItem>();
                if (collectedItems.Contains(item))
                    return;
                collectedItems.Add(item);

                var memberIn = Groups.Values.Where(g => g.Members.Contains(item)).Cast<AccessItem>().ToList();

                Dictionary<string, AclTreeNode> explicitAccessInGroup = new Dictionary<string, AclTreeNode>();

                foreach (var g in memberIn) //and recurse the new items. 
                {
                    if (collectedItems.Contains(g))//dont recurse through already resolved groups... allows cirkular references
                        continue;
                    ResolveAccess(g, ++deepLevel, collectedItems);
                    Dictionary<string, AclTreeNode> explicitAccess;
                    if (ExplicitAccess.TryGetValue(g.Name, out explicitAccess))
                    {
                        foreach (var entry in explicitAccess)
                        {
                            string area = entry.Key;
                            string access = entry.Value.Access;
                            AclTreeNode node;
                            if (!explicitAccessInGroup.TryGetValue(area, out node))
                            {
                                explicitAccessInGroup[area] = node = new AclTreeNode(null, area);
                            }
                            node.SetAccess(access, g);
                        }
                    }
                    //resolve rootnode access for each member

                    string rootarea = "RootArea";
                    string acc = GetAccessInArea(g.Name, rootarea, collectedItems).Item1;
                    AclTreeNode r;
                    if (!explicitAccessInGroup.TryGetValue(rootarea, out r))
                    {
                        explicitAccessInGroup[rootarea] = r = new AclTreeNode(null, rootarea);
                    }
                    r.SetAccess(acc, g);
                }

                foreach (var entry in item.Areas.Areas)
                {
                    string area = entry.Key;
                    string access = entry.Value;
                    AclTreeNode node;
                    if (!explicitAccessInGroup.TryGetValue(area, out node))
                    {
                        explicitAccessInGroup[area] = node = new AclTreeNode(null, area);
                    }
                    node.SetAccess(access, item);

                }
                if (explicitAccessInGroup.Count > 0)
                    ExplicitAccess[item.Name] = explicitAccessInGroup;
                if (UseCache)
                {
                    accessResolved.Add(item.Name);
                }
            }


            public void ResolveAccess(string itemName, int deepLevel, HashSet<AccessItem> collectedItems)
            {
                //find all groups that the group is member in. 

                if (deepLevel > 20) return; //prevents circular referense stack overflow

                if (UseCache)
                {
                    if (accessResolved == null)
                        accessResolved = new HashSet<string>();
                    if (accessResolved.Contains(itemName))
                        return;
                }
                //resolve group membership
                //find item
                AccessItem item = FindAccessItem(itemName);
                ResolveAccess(item, deepLevel, collectedItems);
            }

            public class ItemComparer : IEqualityComparer<AccessItem>
            {
                public bool Equals(AccessItem x, AccessItem y)
                {
                    if (x == null && y != null) return false;
                    if (x != null && y == null) return false;
                    if (x == null && y == null) return true;
                    if (x.Name.Equals(y.Name)) return true;
                    return false;
                }

                public int GetHashCode(AccessItem obj)
                {
                    return obj.GetHashCode();
                }
            }

            public void GetGroupsForGroups(AccessItem group, ref List<AccessItem> theList, int deepLevel = 0)
            {
                //find all groups that the group is member in. 

                if (deepLevel > 20) return; //prevents circular referense stack overflow

                var newList = Groups.Values.Where(g => g.Members.Contains(group)).Cast<AccessItem>().ToList();

                List<AccessItem> templist = theList;

                newList.RemoveAll(templist.Contains); //is there already a reference to the group

                theList.AddRange(newList); //add new accessitems

                foreach (var g in newList) //and recurse the new items. 
                {
                    GetGroupsForGroups(g, ref theList, ++deepLevel);
                }
            }
            public string GetFirstArea(string username)
            {
                ResolveAccess(username, 0, null);
                Dictionary<string, AclTreeNode> explicitAccess;

                //browse i rooten?
                foreach (AclTreeNode root in AreaTreeStructure.RootNode.ChildNodes)
                {
                    string access = GetAccessInArea(username, root.Name).Item1;
                    if (AccessLevels.HasAccess(AccessLevels.Levels.Browse, access))
                        return root.Name;
                }

                //nähä, kanske användaren har explicit access någonstans?

                if (!ExplicitAccess.TryGetValue(username, out explicitAccess))
                    return null; // användaern har inte access nånstans..., inte ens browse...


                var Areas =
                    explicitAccess.Where(n => AccessLevels.HasAccess(AccessLevels.Levels.Browse, n.Value.Access)).Select(
                        n => n.Key).ToList();
                string firstArea = null;
                if (FindFirstArea(AreaTreeStructure.RootNode, ref firstArea, ref Areas))
                    return firstArea;
                return null;

            }

            private bool FindFirstArea(AclTreeNode node, ref string firstArea, ref List<string> areas)
            {
                if (areas.Contains(node.Name))
                {
                    firstArea = node.Name;
                    return true;
                }

                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    if (FindFirstArea(node.ChildNodes[i] as AclTreeNode, ref firstArea, ref areas))
                        return true;
                }

                return false;
            }
        }
    }

