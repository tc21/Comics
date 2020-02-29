using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Python.Runtime;

namespace Comics.Extensions {
    /* 
AbsoluteThumbnailSource = D:\ACG\Bilibili\CNOW\2017OWPS春季赛总决赛 1246 VS LGD(3)\19165471-1.flv
Author = CNOW
Category = Bilibili
CreateThumbnailAndReturnLocation = <bound method 'CreateThumbnailAndReturnLocation'>
CreateUniqueIdentifier = <bound method 'CreateUniqueIdentifier'>
DateAdded = 2019-10-31 05:08:39
DefaultSortIndex = 0
Disliked = False
Equals = <bound method 'Equals'>
ExecutionString = <class 'Comics.ExecutionString'>
FilePaths = Comics.Comic+<RetrieveFilesForComicAtPath>d__34
Finalize = <bound method 'Finalize'>
GenerateThumbnail = <bound method 'GenerateThumbnail'>
GetHashCode = <bound method 'GetHashCode'>
GetType = <bound method 'GetType'>
Loved = False
MatchesAuthors = <bound method 'MatchesAuthors'>
MatchesCategories = <bound method 'MatchesCategories'>
MatchesSearchText = <bound method 'MatchesSearchText'>
MatchesTags = <bound method 'MatchesTags'>
MemberwiseClone = <bound method 'MemberwiseClone'>
Metadata = Comics.Metadata
Open = <bound method 'Open'>
OpenContainingFolder = <bound method 'OpenContainingFolder'>
Overloads = Comics.Comic(System.String, System.String, System.String, System.String, Comics.Metadata, Boolean)
PropertyChanged = <bound event 'PropertyChanged'>
Random = 459098722
RandomSortIndex = 3
ReferenceEquals = <bound method 'ReferenceEquals'>
Save = <bound method 'Save'>
SortDescriptionPropertiesForIndex = <bound method 'SortDescriptionPropertiesForIndex'>
SortProperties = [<Comics.SortPropertyInfo object at 0x0000013F29B7DDA0>, <Comics.SortPropertyInfo object at 0x0000013F29B7DDD8>, <Comics.SortPropertyInfo object at 0x0000013F29B7DE10>, <Comics.SortPropertyInfo object at 0x0000013F29B7DE48>, <Comics.SortPropertyInfo object at 0x0000013F29B7DE80>]
SortPropertyInfo = <class 'Comics.SortPropertyInfo'>
SortPropertyNames = System.Linq.Enumerable+WhereSelectListIterator`2[Comics.Comic+SortPropertyInfo,System.String]
TagString = 
Tags = System.Collections.Generic.HashSet`1[System.String]
TestExecutionString = <bound method 'TestExecutionString'>
ThumbnailPath = C:\Users\lanxia\AppData\Local\TC-C7\Comics\thumbnails\[CNOW]2017OWPS春季赛总决赛 1246 VS LGD(3).thumbnail.jpg
ThumbnailSource = D:\ACG\Bilibili\CNOW\2017OWPS春季赛总决赛 1246 VS LGD(3)\19165471-1.flv
Title = 2017OWPS春季赛总决赛 1246 VS LGD(3)
ToString = <bound method 'ToString'>
UniqueHashCode = <bound method 'UniqueHashCode'>
UniqueIdentifier = [CNOW]2017OWPS春季赛总决赛 1246 VS LGD(3)
__call__ = <method-wrapper '__call__' of Comic object at 0x0000013F29B7D4E0>
__class__ = <class 'Comics.Comic'>
__delattr__ = <method-wrapper '__delattr__' of Comic object at 0x0000013F29B7D4E0>
__delitem__ = <method-wrapper '__delitem__' of Comic object at 0x0000013F29B7D4E0>
__dir__ = <built-in method __dir__ of Comic object at 0x0000013F29B7D4E0>
__doc__ = Void .ctor(System.String, System.String, System.String, System.String, Comics.Metadata, Boolean)
__eq__ = <method-wrapper '__eq__' of Comic object at 0x0000013F29B7D4E0>
__format__ = <built-in method __format__ of Comic object at 0x0000013F29B7D4E0>
__ge__ = <method-wrapper '__ge__' of Comic object at 0x0000013F29B7D4E0>
__getattribute__ = <method-wrapper '__getattribute__' of Comic object at 0x0000013F29B7D4E0>
__getitem__ = <method-wrapper '__getitem__' of Comic object at 0x0000013F29B7D4E0>
__gt__ = <method-wrapper '__gt__' of Comic object at 0x0000013F29B7D4E0>
__hash__ = <method-wrapper '__hash__' of Comic object at 0x0000013F29B7D4E0>
__init__ = <method-wrapper '__init__' of Comic object at 0x0000013F29B7D4E0>
__init_subclass__ = <built-in method __init_subclass__ of CLR Metatype object at 0x0000013F27B8BA78>
__iter__ = <method-wrapper '__iter__' of Comic object at 0x0000013F29B7D4E0>
__le__ = <method-wrapper '__le__' of Comic object at 0x0000013F29B7D4E0>
__lt__ = <method-wrapper '__lt__' of Comic object at 0x0000013F29B7D4E0>
__module__ = Comics
__ne__ = <method-wrapper '__ne__' of Comic object at 0x0000013F29B7D4E0>
__new__ = <built-in method __new__ of CLR Metatype object at 0x0000013F27B8BA78>
__overloads__ = Comics.Comic(System.String, System.String, System.String, System.String, Comics.Metadata, Boolean)
__reduce__ = <built-in method __reduce__ of Comic object at 0x0000013F29B7D4E0>
__reduce_ex__ = <built-in method __reduce_ex__ of Comic object at 0x0000013F29B7D4E0>
__repr__ = <method-wrapper '__repr__' of Comic object at 0x0000013F29B7D4E0>
__setattr__ = <method-wrapper '__setattr__' of Comic object at 0x0000013F29B7D4E0>
__setitem__ = <method-wrapper '__setitem__' of Comic object at 0x0000013F29B7D4E0>
__sizeof__ = <built-in method __sizeof__ of Comic object at 0x0000013F29B7D4E0>
__str__ = <method-wrapper '__str__' of Comic object at 0x0000013F29B7D4E0>
__subclasshook__ = <built-in method __subclasshook__ of CLR Metatype object at 0x0000013F27B8BA78>
add_PropertyChanged = <bound method 'add_PropertyChanged'>
get_AbsoluteThumbnailSource = <bound method 'get_AbsoluteThumbnailSource'>
get_Author = <bound method 'get_Author'>
get_Category = <bound method 'get_Category'>
get_DateAdded = <bound method 'get_DateAdded'>
get_Disliked = <bound method 'get_Disliked'>
get_FilePaths = <bound method 'get_FilePaths'>
get_Loved = <bound method 'get_Loved'>
get_Metadata = <bound method 'get_Metadata'>
get_Random = <bound method 'get_Random'>
get_TagString = <bound method 'get_TagString'>
get_Tags = <bound method 'get_Tags'>
get_ThumbnailPath = <bound method 'get_ThumbnailPath'>
get_ThumbnailSource = <bound method 'get_ThumbnailSource'>
get_Title = <bound method 'get_Title'>
get_UniqueIdentifier = <bound method 'get_UniqueIdentifier'>
path = D:\ACG\Bilibili\CNOW\2017OWPS春季赛总决赛 1246 VS LGD(3)
real_author = CNOW
real_category = Bilibili
real_title = 2017OWPS春季赛总决赛 1246 VS LGD(3)
remove_PropertyChanged = <bound method 'remove_PropertyChanged'>
set_Author = <bound method 'set_Author'>
set_Category = <bound method 'set_Category'>
set_Disliked = <bound method 'set_Disliked'>
set_Loved = <bound method 'set_Loved'>
set_Metadata = <bound method 'set_Metadata'>
set_Random = <bound method 'set_Random'>
set_TagString = <bound method 'set_TagString'>
set_ThumbnailSource = <bound method 'set_ThumbnailSource'>
set_Title = <bound method 'set_Title'>
         */
    public static class PythonnetExtensions {
        static PythonnetExtensions() {
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH").Replace("Python38", "Python37"));
        }

        public static void ExecuteCommand(object sender, ExecutedRoutedEventArgs e, string extensionFileName) {
            if (sender is ListBox listBox) {
                using (Py.GIL()) {
                    dynamic comics = new PyList();
                    foreach (var item in listBox.SelectedItems) {
                        if (item is Comic comic) {
                            var pyobject = comic.ToPython();
                            comics.append(pyobject);
                        }
                    }

                    using (var scope = Py.CreateScope()) {
                        /* 
                         * What we have right now is a python script that runs at exactly the authority of the C# code
                         * surrounding this comment. That is too dangerous. We will create a custom object to pass to the
                         * Python script, and retrieve the object, detect changes, and apply them to our works.
                         * 
                         * This not only makes things safer and simpler, but also ensures that if the python script crashes, 
                         * no changes will be made to anything in the database.
                         */
                        try {
                            scope.Exec(System.IO.File.ReadAllText(extensionFileName));
                            dynamic extension_func = scope.Get("extension");
                            string result = extension_func(comics);
                            MessageBox.Show(result);
                        } catch (PythonException error) {
                            MessageBox.Show("An error occured while running the extension: " + error.ToString());
                        } catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) {
                            MessageBox.Show("An error occured while running the extension: The extension must define a function extension(List[Comic]) -> str. Did you define the function wrongly?");
                        }
                    }
                }
            } else {
                MessageBox.Show("Command execution failed! Unable to cast sender of command as ListBox");
            }
        }
    }
}
