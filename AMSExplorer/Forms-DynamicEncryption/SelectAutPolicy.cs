﻿//----------------------------------------------------------------------------------------------
//    Copyright 2016 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//---------------------------------------------------------------------------------------------


using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.IO;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
using Microsoft.WindowsAzure.MediaServices.Client.ContentKeyAuthorization;

namespace AMSExplorer
{
    public partial class SelectAutPolicy : Form
    {
        private CloudMediaContext _context;
        private List<IContentKeyAuthorizationPolicy> autPolicies;

        public IContentKeyAuthorizationPolicy SelectedPolicy
        {
            get
            {
                if (listViewPolicies.SelectedIndices.Count > 0)
                {
                    return autPolicies.Skip(listViewPolicies.SelectedIndices[0]).Take(1).FirstOrDefault();
                }
                else
                {
                    return null;
                }
            }
        }


        public SelectAutPolicy(CloudMediaContext context)
        {
            InitializeComponent();
            this.Icon = Bitmaps.Azure_Explorer_ico;
            _context = context;
        }

        private void EncodingAMEStandardPickOverlay_Load(object sender, EventArgs e)
        {
            ListPolicies();
        }

        private void ListPolicies()
        {
            listViewPolicies.Items.Clear();

            autPolicies = _context.ContentKeyAuthorizationPolicies.ToList();

            listViewPolicies.BeginUpdate();
            foreach (var pol in autPolicies)
            {
                ListViewItem item = new ListViewItem(pol.Name, 0);
                item.SubItems.Add(pol.Id);
                listViewPolicies.Items.Add(item);
            }
            listViewPolicies.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listViewPolicies.EndUpdate();

        }


        private void listViewFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonSelect.Enabled = listViewPolicies.SelectedItems.Count > 0;
            DoDisplayKeyPropertiesAndAutOptions();
        }


        private void DoDisplayKeyPropertiesAndAutOptions()
        {
            listViewAutPolOptions.Items.Clear();
            dataGridViewAutPolOption.Rows.Clear();
            var myAuthPolicy = SelectedPolicy;
            if (myAuthPolicy != null)
            {
                listViewAutPolOptions.BeginUpdate();
                foreach (var option in myAuthPolicy.Options)
                {
                    ListViewItem item = new ListViewItem((string.IsNullOrEmpty(option.Name) ? "<no name>" : option.Name), 0);
                    listViewAutPolOptions.Items.Add(item);
                }
                listViewAutPolOptions.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                listViewAutPolOptions.EndUpdate();
                if (listViewAutPolOptions.Items.Count > 0) listViewAutPolOptions.Items[0].Selected = true;
            }
        }



        private void listViewAutPolOptions_SelectedIndexChanged(object sender, EventArgs e)
        {
            DoDisplayAuthorizationPolicyOption();
        }

        private void DoDisplayAuthorizationPolicyOption()
        {

            if (listViewAutPolOptions.SelectedItems.Count > 0 && SelectedPolicy != null)
            {
                dataGridViewAutPolOption.Rows.Clear();
                dataGridViewAutPolOption.ColumnCount = 2;
                dataGridViewAutPolOption.Columns[0].DefaultCellStyle.BackColor = Color.Gainsboro;


                IContentKeyAuthorizationPolicyOption option = SelectedPolicy.Options.Skip(listViewAutPolOptions.SelectedIndices[0]).Take(1).FirstOrDefault();
                if (option != null) // Token option
                {
                    dataGridViewAutPolOption.Rows.Add("Name", option.Name != null ? option.Name : "<no name>");
                    dataGridViewAutPolOption.Rows.Add("Id", option.Id);

                    // Key delivery configuration

                    int i = dataGridViewAutPolOption.Rows.Add("KeyDeliveryConfiguration", "<null>");
                    if (option.KeyDeliveryConfiguration != null)
                    {
                        DataGridViewButtonCell btn = new DataGridViewButtonCell();
                        dataGridViewAutPolOption.Rows[i].Cells[1] = btn;
                        dataGridViewAutPolOption.Rows[i].Cells[1].Value = "See value";
                        dataGridViewAutPolOption.Rows[i].Cells[1].Tag = option.KeyDeliveryConfiguration;
                    }

                    dataGridViewAutPolOption.Rows.Add("KeyDeliveryType", option.KeyDeliveryType);

                    List<ContentKeyAuthorizationPolicyRestriction> objList_restriction = option.Restrictions;
                    foreach (var restriction in objList_restriction)
                    {
                        dataGridViewAutPolOption.Rows.Add("Restriction Name", restriction.Name);
                        dataGridViewAutPolOption.Rows.Add("Restriction KeyRestrictionType", (ContentKeyRestrictionType)restriction.KeyRestrictionType);
                        
                        if (restriction.Requirements != null)
                        {
                            // Restriction Requirements
                            i = dataGridViewAutPolOption.Rows.Add("Restriction Requirements", "<null>");
                            if (restriction.Requirements != null)
                            {
                                DataGridViewButtonCell btn2 = new DataGridViewButtonCell();
                                dataGridViewAutPolOption.Rows[i].Cells[1] = btn2;
                                dataGridViewAutPolOption.Rows[i].Cells[1].Value = "See value";
                                dataGridViewAutPolOption.Rows[i].Cells[1].Tag = restriction.Requirements;

                                TokenRestrictionTemplate tokenTemplate = TokenRestrictionTemplateSerializer.Deserialize(restriction.Requirements);
                                dataGridViewAutPolOption.Rows.Add("Token Type", tokenTemplate.TokenType);

                                i = dataGridViewAutPolOption.Rows.Add("Primary Verification Key", "<null>");
                                if (tokenTemplate.PrimaryVerificationKey != null)
                                {
                                    dataGridViewAutPolOption.Rows.Add("Token Verification Key Type", (tokenTemplate.PrimaryVerificationKey.GetType() == typeof(SymmetricVerificationKey)) ? "Symmetric" : "Asymmetric (X509)");
                                    if (tokenTemplate.PrimaryVerificationKey.GetType() == typeof(SymmetricVerificationKey))
                                    {
                                        var verifkey = (SymmetricVerificationKey)tokenTemplate.PrimaryVerificationKey;
                                        btn2 = new DataGridViewButtonCell();
                                        dataGridViewAutPolOption.Rows[i].Cells[1] = btn2;
                                        dataGridViewAutPolOption.Rows[i].Cells[1].Value = "See key value";
                                        dataGridViewAutPolOption.Rows[i].Cells[1].Tag = Convert.ToBase64String(verifkey.KeyValue);
                                    }
                                }


                                foreach (var verifkey in tokenTemplate.AlternateVerificationKeys)
                                {
                                    i = dataGridViewAutPolOption.Rows.Add("Alternate Verification Key", "<null>");
                                    if (verifkey != null)
                                    {
                                        dataGridViewAutPolOption.Rows.Add("Token Verification Key Type", (verifkey.GetType() == typeof(SymmetricVerificationKey)) ? "Symmetric" : "Asymmetric (X509)");
                                        if (verifkey.GetType() == typeof(SymmetricVerificationKey))
                                        {
                                            var verifkeySym = (SymmetricVerificationKey)verifkey;
                                            btn2 = new DataGridViewButtonCell();
                                            dataGridViewAutPolOption.Rows[i].Cells[1] = btn2;
                                            dataGridViewAutPolOption.Rows[i].Cells[1].Value = "See key value";
                                            dataGridViewAutPolOption.Rows[i].Cells[1].Tag = Convert.ToBase64String(verifkeySym.KeyValue);
                                        }
                                    }
                                }

                                if (tokenTemplate.OpenIdConnectDiscoveryDocument != null)
                                {
                                    dataGridViewAutPolOption.Rows.Add("OpenId Connect Discovery Document Uri", tokenTemplate.OpenIdConnectDiscoveryDocument.OpenIdDiscoveryUri);
                                }
                                dataGridViewAutPolOption.Rows.Add("Token Audience", tokenTemplate.Audience);
                                dataGridViewAutPolOption.Rows.Add("Token Issuer", tokenTemplate.Issuer);
                                foreach (var claim in tokenTemplate.RequiredClaims)
                                {
                                    dataGridViewAutPolOption.Rows.Add("Required Claim, Type", claim.ClaimType);
                                    dataGridViewAutPolOption.Rows.Add("Required Claim, Value", claim.ClaimValue);
                                }
                            }
                        }
                    }
                }
               
            }
            
        }

    }
}
