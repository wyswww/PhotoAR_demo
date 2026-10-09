package com.photoar;
import android.app.Fragment;
import android.app.Activity;
import android.os.Bundle;
import android.content.Intent;
import android.net.Uri;
import com.unity3d.player.UnityPlayer;
import java.io.InputStream;
import java.io.File;
import java.io.FileOutputStream;

public class Gallery extends Fragment {
    public static String cachedPhotoPath() {
        File file=new File(UnityPlayer.currentActivity.getCacheDir(),"photoar-selected-image");
        return file.isFile()?file.getAbsolutePath():"";
    }
    private String target;
    public static void pick(final String target) {
        final Activity activity=UnityPlayer.currentActivity;
        activity.runOnUiThread(() -> {
            Gallery picker=new Gallery();Bundle args=new Bundle();args.putString("target",target);picker.setArguments(args);
            activity.getFragmentManager().beginTransaction().add(picker,"photoar-gallery").commit();
        });
    }
    @Override public void onCreate(Bundle saved) {
        super.onCreate(saved);target=getArguments().getString("target");
        if(saved==null) {
            Intent intent=new Intent(Intent.ACTION_OPEN_DOCUMENT);intent.setType("image/*");intent.addCategory(Intent.CATEGORY_OPENABLE);
            startActivityForResult(intent,91);
        }
    }
    @Override public void onActivityResult(int request,int result,Intent data) {
        if(request!=91)return;
        try {
            if(result!=Activity.RESULT_OK||data==null){UnityPlayer.UnitySendMessage(target,"OnPhotoError","Photo selection cancelled");return;}
            Uri uri=data.getData();File out=new File(getActivity().getCacheDir(),"photoar-selected-image");
            try(InputStream in=getActivity().getContentResolver().openInputStream(uri);FileOutputStream dest=new FileOutputStream(out)) {
                byte[] buffer=new byte[8192];int count;long total=0;
                while((count=in.read(buffer))!=-1){total+=count;if(total>12*1024*1024)throw new Exception("Image exceeds 12 MB");dest.write(buffer,0,count);}
            }
            UnityPlayer.UnitySendMessage(target,"OnPhotoSelected",out.getAbsolutePath());
        } catch(Exception e){UnityPlayer.UnitySendMessage(target,"OnPhotoError",e.toString());}
        finally {getActivity().getFragmentManager().beginTransaction().remove(this).commitAllowingStateLoss();}
    }
}
