package com.photoar;
import com.google.ar.core.ArCoreApk;
import com.google.ar.core.Session;
import com.unity3d.player.UnityPlayer;

public class ARCoreProbe {
    // Constructor validates the actual device/profile without opening a second camera stream.
    public static String inspect() {
        String availability=ArCoreApk.getInstance().checkAvailability(UnityPlayer.currentActivity).toString();
        try {
            Session session=new Session(UnityPlayer.currentActivity);
            int configs=session.getSupportedCameraConfigs().size();session.close();
            return availability+"; session constructor OK; camera configs="+configs;
        }catch(Exception error){return availability+"; "+error.getClass().getName()+": "+error.getMessage();}
    }
}
