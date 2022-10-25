/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.  

See SampleFramework license.txt for license terms.  Unless required by applicable law 
or agreed to in writing, the sample code is provided “AS IS” WITHOUT WARRANTIES OR 
CONDITIONS OF ANY KIND, either express or implied.  See the license for specific 
language governing permissions and limitations under the license.

************************************************************************************/

using System;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonRigidbodyView))]
public class NetDistanceGrabbable : OVRGrabbable
{
    public string m_materialColorField;

    NetGrabbableCrosshair m_crosshair;
    NetGrabManager m_crosshairManager;
    Renderer m_renderer;
    MaterialPropertyBlock m_mpb;

    // Networking
    PhotonView photonView;

    public bool InRange
    {
        get { return m_inRange; }
        set
        {
            m_inRange = value;
            RefreshCrosshair();
        }
    }
    bool m_inRange;

    public bool Targeted
    {
        get { return m_targeted; }
        set
        {
            m_targeted = value;
            RefreshCrosshair();
        }
    }
    bool m_targeted;

    protected override void Start()
    {
        base.Start();
        m_crosshair = gameObject.GetComponentInChildren<NetGrabbableCrosshair>();
        m_renderer = gameObject.GetComponent<Renderer>();
        m_crosshairManager = FindObjectOfType<NetGrabManager>();
        m_mpb = new MaterialPropertyBlock();
        RefreshCrosshair();
        m_renderer.SetPropertyBlock(m_mpb);

        // Networking
        photonView = GetComponent<PhotonView>();
    }

    void RefreshCrosshair()
    {
        if (m_crosshair)
        {
            if (isGrabbed) m_crosshair.SetState(NetGrabbableCrosshair.CrosshairState.Disabled);
            else if (!InRange) m_crosshair.SetState(NetGrabbableCrosshair.CrosshairState.Disabled);
            else m_crosshair.SetState(Targeted ? NetGrabbableCrosshair.CrosshairState.Targeted : NetGrabbableCrosshair.CrosshairState.Enabled);
        }
        if (m_materialColorField != null && m_crosshairManager != null)
        {
            m_renderer.GetPropertyBlock(m_mpb);
            if (isGrabbed || !InRange) m_mpb.SetColor(m_materialColorField, m_crosshairManager.OutlineColorOutOfRange);
            else if (Targeted) m_mpb.SetColor(m_materialColorField, m_crosshairManager.OutlineColorHighlighted);
            else m_mpb.SetColor(m_materialColorField, m_crosshairManager.OutlineColorInRange);
            m_renderer.SetPropertyBlock(m_mpb);
        }
    }

    public override void GrabBegin(OVRGrabber hand, Collider grabPoint)
    {
        base.GrabBegin(hand, grabPoint);

        // Request ownership and send RPC
        if (photonView != null && !photonView.IsMine)
        {
            photonView.RequestOwnership();
            Debug.Log("Requesting ownership...");
            photonView.RPC(nameof(SetGrabKinematic), RpcTarget.Others, true);
        }
    }

    public override void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        base.GrabEnd(linearVelocity, angularVelocity);

        // Send RPC
        if (photonView != null)
        {
            photonView.RPC(nameof(SetGrabKinematic), RpcTarget.Others, m_grabbedKinematic);
        }

    }

    [PunRPC]
    void SetGrabKinematic(bool grabbed)
    {
        gameObject.GetComponent<Rigidbody>().isKinematic = grabbed;
    }
}
