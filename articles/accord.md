# Accord.NET

This library is built on top of the excellent [Accord.NET framework](http://accord-framework.net/).

Accord.NET is [licensed](http://accord-framework.net/license.html) under the "GNU Lesser Public License v2.1".
It can be used in commercial applications "as long as you are only linking unmodified .dll files and mention in your software and relevant source
material that you are using this framework" - like I am doing right here. 

**Thanks to the developers of Accord.NET for a great framework!**

No modifications are made or will ever be made to the Accord.NET dependencies in this library.

Development of Accord.NET has stopped at version 3.8.0, but I see this as no reason to worry, as the methods that are used are very stable - actually, you would not expect a 
mathematics library to need to continue development indefinitely. 

In versions ``1.x`` of ``TimeSeriesAnalysis``, Accord.NET is only used inside ``Vec.Regress``, and so removing the dependency to Accord.NET would only require this one method to be ported. 

Accord.NET has implemented **classification**, **clustering**, **kernel methods** and **hypothesis testing** that may become useful in future work within data-mining and building larger scale models toward version ``2.x``. 

