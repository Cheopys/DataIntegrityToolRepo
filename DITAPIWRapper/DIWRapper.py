import clr
# Adding reference to class library!
clr.AddReference('DLL.dll')
# Importing specific class from this namespace, here!
from DLL import wrapper
# Calling functions using class name, since these all are static!
def pmethod1():
    str = wrapper.method1()
def pmethod2():
    inte = wrapper.method2()
def pmethod3():
    doub = wrapper.method3()

