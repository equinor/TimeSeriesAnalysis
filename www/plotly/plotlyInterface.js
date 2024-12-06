// can parse create either
// a) time-series plots with up to two subplots and up to four y-axes, (each time-series in seprate csv-file) or
// b) x-y scatterplot (data must be in a single xy-plot)
// how to plot data is determined by parsing the hash of the url (the part after "#")

function makePlotlyPlotFromCSV(hash)
{
    // one or more csvs will be loaded and placed on either left or right y-axis deepening on format of hash
    // reading each csv is asynchronous, and plotting is done only after reading all csvs has completed...
    var DataStorage = function(hash){
		this.hash=hash; 
		this.data = [];
		this.csvnames=[];
		this.hasTwoSubplots; 
        this.isVarOnY1=[];
		this.isVarOnY2=[];
		this.isVarOnY3=[];
		this.isVarOnY4=[];
		this.comment="";
		this.casename=""
		this.isXY = false;
		this.isXYwithMarkers = [];
		this.isXYwithLine = [];
		};// create object
    DataStorage.prototype.addData = function (csvname,dataIn) 
    {
        this.data[csvname] = dataIn; 
    }
    DataStorage.prototype.getData = function (csvname) 
    {
        return this.data[csvname];
    }
    DataStorage.prototype.parseHash = function () 
    {
        var splitStr   = this.hash.split(";");
        this.nVariablesToPlot=0;
        for (var i=0; i<splitStr.length; i++)
        {
			if (splitStr[i].indexOf("comment:")==-1 && splitStr[i].indexOf("comment=")==-1 && 
				splitStr[i].indexOf("casename:")==-1)//ignore "comment= "field
			{
				if (splitStr[i].includes("xy="))
				{
					this.isXY=true;
					this.isXYwithMarkers.push(false);
					this.isXYwithLine.push(false);
				}
				else if (splitStr[i].includes("xym="))
				{
					this.isXY=true;
					this.isXYwithMarkers.push(true);
					this.isXYwithLine.push(false);
				}
				else if (splitStr[i].includes("xyl="))
				{
					this.isXY=true;
					this.isXYwithMarkers.push(false);
					this.isXYwithLine.push(true);
				}
				else if (splitStr[i].includes("y2="))
				{ 
					this.isVarOnY1.push(false);
					this.isVarOnY2.push(true);
					this.isVarOnY3.push(false);
					this.isVarOnY4.push(false);
				}
				else if (splitStr[i].includes("y3="))
				{ 
					this.isVarOnY1.push(false);
					this.isVarOnY2.push(false);
					this.isVarOnY3.push(true);
					this.isVarOnY4.push(false);
					this.hasTwoSubplots = true;
				}
				else if (splitStr[i].includes("y4="))
				{ 
					this.isVarOnY1.push(false);
					this.isVarOnY2.push(false);
					this.isVarOnY3.push(false);
					this.isVarOnY4.push(true);
					this.hasTwoSubplots = true;
				}
				else
				{
					this.isVarOnY1.push(true);
					this.isVarOnY2.push(false);
					this.isVarOnY3.push(false);
					this.isVarOnY4.push(false);
				}
				this.nVariablesToPlot++;
				this.csvnames.push(splitStr[i].replace("y1=","").replace("y2=","").
					replace("y3=","").replace("y4=","").
					replace("xy=","").replace("xym=","").replace("xyl=","")
				);
			}
			else
			{
				if (splitStr[i].indexOf("casename:")>-1)
					this.casename = splitStr[i].replace("casename:","");
				else
					this.comment = splitStr[i].replace("comment=","").replace("comment:","");
			}
			
        }
		if (this.caseName !== "")
		{
			for (var i=0; i<this.nVariablesToPlot; i++)
			{
				this.csvnames[i]= this.casename +"__"+ this.csvnames[i];
			}
		}
        for (var i=0; i<this.nVariablesToPlot; i++)
        {
            this.data[this.csvnames[i]]= null;
        }
        return;
    }
    DataStorage.prototype.HasAllDataArrived = function () 
    {
        var hasAllDataArrived=true;
        for (var i=0; i<this.nVariablesToPlot; i++)
        {
            var csvname = this.csvnames[i];
            if (this.data[csvname]== null)
                hasAllDataArrived = false;
        }
        return hasAllDataArrived;
    }

    var storageObj= new DataStorage(hash);
    storageObj.parseHash();

    for (let i=0; i<storageObj.csvnames.length; i++)
    {
        let csvname = storageObj.csvnames[i];
		if ( typeof csvname === 'undefined')
		{
			AddErrorMessage("Error: undefined variable found in hash ");
		}
		try
		{
			Plotly.d3.csv("data//"+csvname+".csv",  
				function(data)
				{
					console.log(csvname+ "read" );
					csvCallBackFunction(data,csvname,storageObj.addData,storageObj); 
					var hasAllArrived = callbackCreatePlotIfAllDataIsHere(storageObj);
				}
			);
		}
		catch(error) 
		{
			AddErrorMessage("an error occurred attempting to read file data//"+csvname );
		}	
		
    }
    // this will not work, as we need to wait for asynchronous filereader to finsish reading csv...
  // makePlotlyPlotFromCSV_inner(storageObj.get())

    // create back button
    var newDiv = document.createElement("div"); 
    // and give it some content 
    var button = document.createElement("button"); 
    button.innerHTML = "Back";
    // add the text node to the newly created div
    newDiv.appendChild(button);
    // add the newly created element and its content into the DOM 
    var currentDiv = document.getElementById("ButtonDiv"); 
    document.body.insertBefore(newDiv, currentDiv); 
    button.addEventListener ("click", function() { window.location='javascript:history.back()';    });

   // Plotly.d3.csv("data//"+name+".csv", function(data){ makePlotlyPlotFromCSV_inner(data) } )
}	
    
function AddErrorMessage(errorString )
{
	var newDiv = document.createElement("div"); 
	// and give it some content 
	var errorMsg = document.createElement("P"); 
	errorMsg.innerHTML = errorString;
	newDiv.appendChild(errorMsg);	
	var currentDiv = document.getElementById("TableDiv"); 
    document.body.insertBefore(newDiv, currentDiv); 
}	
function csvCallBackFunction(data,csvname,callback,callbackObj)
{
    callback.call(callbackObj,csvname,data);
} 

function callbackCreatePlotIfAllDataIsHere(dataStorageObj)
{
    var hasAllArrived = dataStorageObj.HasAllDataArrived();
    if (dataStorageObj.HasAllDataArrived())
	{
		if (dataStorageObj.isXY == true)
			makePlotyXYPlot(dataStorageObj);
		else
			makePlotyTimeSeriesPlot(dataStorageObj);
	}
    return hasAllArrived;
}


function makePlotyXYPlot(dataStorageObj)
{
    function unpack(rows, key)
    {
	  	return rows.map(function(row) { return row[key]; });	// map: convert string of number to double..
    }
	function unpackDouble(rows, key)
    {
	  	return rows.map(function(row) { return parseFloat(row[key]); });	// map: convert string of number to double..
    }

	// load data
	var data = new Array();
	for(let csvIdx=0;csvIdx<dataStorageObj.nVariablesToPlot;csvIdx++)
	{
		var csvname = dataStorageObj.csvnames[csvIdx];
		var allRows = dataStorageObj.getData(csvname);
		var columnNameArray = Object.keys(allRows[0]);
		let trace1 = {
			x: unpackDouble(allRows,columnNameArray[1]),
			y: unpackDouble(allRows,columnNameArray[2]),
			mode: 'markers',
			type: 'scatter',
			name: csvname.replace(dataStorageObj.casename+"__",'').replace(".csv",""),
			text: unpack(allRows,columnNameArray[0]),
			  textfont : {family:'Times New Roman'},
			  textposition: 'bottom center',
			  marker: { size: 12 }
		};
		if(dataStorageObj.isXYwithMarkers[csvIdx]==true)
		{
			trace1.mode = 'markers+text';
		}
		if(dataStorageObj.isXYwithLine[csvIdx]==true)
		{
			trace1.mode = 'lines';
			trace1.text = '';
		}
		data.push(trace1);
	}
	// load layout
	var layout = {
		title:  columnNameArray[2] ,
		xaxis: {title: columnNameArray[1]}
	};
	if(dataStorageObj.comment)
	{
		layout.title+="("+dataStorageObj.comment+")";
	}
	Plotly.newPlot('PlotDiv', data,layout);
}

function makePlotyTimeSeriesPlot(dataStorageObj)
{
    // local function
    function unpack(rows, key)
    {
	  	return rows.map(function(row) { return row[key]; });	// map: convert string of number to double..
    }
    function unpackDate(rows, key)
    {
	  	return rows.map(function(row) { return ConvertUnixTimeStringToTimeString(row[key]); });	// map: convert string of number to double..
    }
    // 'yyyy-mm-dd HH:MM:SS.ssssss'
    function ConvertUnixTimeStringToTimeString(unix_timestamp)
    {  
        var minpadding='';
        var hourpadding='';
        var secpadding ='';
        var unix_timestamp_int = parseInt(unix_timestamp)*1000;
        var a       = new Date(unix_timestamp_int );
        var year    = a.getFullYear();
        var month   = a.getMonth()+1;// zero indicates first mont of the year!!!
        var date    = a.getDate();
        var hour    = a.getHours();
        var min     = a.getMinutes();
        var sec     = a.getSeconds();
        if (min<10)
            minpadding ='0';
        if (hour<10)
            hourpadding='0';
        if (sec<10)
            secpadding='0';    
            var time = year + '-' + month + '-' +date + ' ' +hourpadding+ hour + ':' +minpadding+ min + ':' +secpadding+ sec ;
        // Will display time in 10:30:23 format
        return time;
    }
    //-----------------------------------------
    // expect date-value-date2-value2-date3-value etc, but each date/value pair can have different number 
    // of rows!
    var data = new Array();
    for (let csvIdx=0;csvIdx<dataStorageObj.nVariablesToPlot;csvIdx++)
    {
        var csvname = dataStorageObj.csvnames[csvIdx];
        var allRows = dataStorageObj.getData(csvname);
		
		if (allRows.length == 0)
		{
			AddErrorMessage("<b>Error:</b> no data in data//"+csvname+".csv?");
			continue;
		}
	
        var columnNameArray = Object.keys(allRows[0]);

        for (var columnIdx=0; columnIdx<columnNameArray.length-1;columnIdx=columnIdx+2)
        {
            let currentDateColumnName = columnNameArray[columnIdx];
            let currentValueColumnName = columnNameArray[columnIdx+1];
            if (csvIdx==0)
            {
                if (columnIdx == 0)
                    colorName = "SteelBlue";
                else if (columnIdx <= 6)
                    colorName = "IndianRed";
                else if (columnIdx <= 12)
                    colorName = "DarkOliveGreen";
                else if (columnIdx <= 18)
                    colorName = "GoldenRod";
                else if (columnIdx <= 24)
                    colorName = "DarkOrchid";
				else if (columnIdx <= 30)
					 colorName = "RosyBrown";
				else if (columnIdx <= 36)
					 colorName = "Black";
				else if (columnIdx <= 42)
					 colorName = "DarkGray";
				else if (columnIdx <= 48)
					 colorName = "IndianRed";
            }else if(csvIdx==1)
            {   colorName  ="RosyBrown";}
            else if (csvIdx==2)
            {   colorName  ="Black";}
            else if (csvIdx==3)
            {   colorName  ="DarkGray";}
		    else if (csvIdx==4)
            {   colorName  ="IndianRed";}
		    else if (csvIdx==5)
            {   colorName  ="DarkOliveGreen";}
		    else if (csvIdx==6)
            {   colorName  ="GoldenRod";}
		    else if (csvIdx==7)
            {   colorName  ="Brown";}
			else if (csvIdx==8)
            {   colorName  ="CadetBlue";}
			else if (csvIdx==9)
            {   colorName  ="Violet";}
			else if (csvIdx==10)
            {   colorName  ="DarkSlateBlue";}
			else if (csvIdx==11)
            {   colorName  ="LightPink";}
			else if (csvIdx==12)
            {   colorName  ="YellowGreen";}
			else if (csvIdx==13)
            {   colorName  ="Olive";}
			else if (csvIdx==14)
            {   colorName  ="DarkCyan";}
			else if (csvIdx==15)
            {   colorName  ="MediumSpringGreen";}

            let prettyName = currentValueColumnName.replace(dataStorageObj.casename+"__",'');
            if (prettyName == "price")
                prettyName = csvname;
            let trace1 = {
                x: unpackDate(allRows, currentDateColumnName),// 'yyyy-mm-dd HH:MM:SS.ssssss'
                y: unpack(allRows, currentValueColumnName),
                mode: 'lines',
                name: prettyName,
                line: { width: 2 } // color: colorName,
            };
            if (csvIdx < 16)
            {
                  trace1.line =  { color: colorName, width: 2 }
            }

            if (dataStorageObj.isVarOnY1[csvIdx]==true)
            {
                trace1.yaxis = 'y1';
                trace1.xaxis = 'x1';
            }
            if (dataStorageObj.isVarOnY2[csvIdx]==true)
            {
                trace1.yaxis ='y2';
                trace1.xaxis = 'x1';
            }
            if (dataStorageObj.isVarOnY3[csvIdx]==true)
            {
                trace1.yaxis ='y3';
                trace1.xaxis = 'x1';
            }
            if (dataStorageObj.isVarOnY4[csvIdx]==true)
            {
                trace1.yaxis ='y4';
                trace1.xaxis ='x1';
            }
           data.push(trace1);
        }
    }

    var layout = {};
 //  layout.autosize = true;
    layout.hovermode= 'x'; 
    if( dataStorageObj.hasTwoSubplots)
    {
        layout.xaxis1  = { domain: [0, 1],                    anchor: 'y1'/*, range: ['2007',Date.now()]*/,type: 'date',side: 'top', },
        layout.xaxis2  = { domain: [0, 1], overlaying:'x1',   anchor: 'y3'/*, range: ['2007',Date.now()]*/,type: 'date' },  
        layout.yaxis1 =  { domain: [0.50, 1],                   side: 'left',  anchor: 'x1' }
        layout.yaxis2 =  { domain: [0.50, 1],overlaying: 'y1',  side: 'right', anchor: 'x1' }
        layout.yaxis3 =  { domain: [0, 0.5],                side: 'left' , anchor: 'x1' }
        layout.yaxis4 =  { domain: [0.,0.5],overlaying: 'y3',  side: 'right', anchor: 'x1' }
    }
    else
    {
        layout.xaxis1  =  { domain: [0, 1], anchor:'y1'},
        layout.yaxis1 =  { domain: [0, 1],                  side: 'left',  anchor: 'x1' }
        layout.yaxis2 =  { domain: [0, 1],overlaying: 'y1',  side: 'right', anchor: 'x1' }
    }
	layout.title = dataStorageObj.comment;

    Plotly.newPlot('PlotDiv', data,layout);

}
