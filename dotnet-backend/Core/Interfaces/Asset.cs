using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;

public class Asset
{

    public Asset(int metaData, string dbRef) {
        _metaData = metaData;
        _dbRef = dbRef;
    }

    public boolean archiveAsset() {
        // todo

    }

    public boolean setMetaData(Map<string, any> data) {

    }

    public boolean removeTags(List<string> tags) {

    }

    public boolean compress() {
        
    }


}