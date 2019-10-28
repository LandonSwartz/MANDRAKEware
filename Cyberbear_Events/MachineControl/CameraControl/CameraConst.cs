﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cyberbear_Events.MachineControl.CameraControl
{
    /// <summary>
    /// Consts for Camera Control Class
    /// </summary>
    /// May add methods to change the consts
    public static class CameraConst
    {
        public static string CameraState = "Not Ready"; //state of camera 
        public static string CameraSettingsPath = @"C:\Users\sam998\Desktop\Cyberbear\Cyberbear\Cyberbear_Events\Machine\CameraControl\CameraSettings\wednesday_night_special.xml"; //where are camera settings saved
        public static string SaveFolderPath = @"C:\Users\sam998\Desktop\TestPhotosFolder"; //save folder for photo capturing, hardcoded to desktop, remember to change when needed
        public static string FileName = "NewImage"; //name of saved images

    }
}