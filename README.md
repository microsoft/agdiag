**AGDIAG**

       AGDiag diagnoses and reports failover and health events detected in the Cluster log of the primary replica.

       AGDIAG will detect and report on these issues:

           * Detect and analyze Cluster or SQL health issues that cause availability group to fail over or go offline.
           * Detect and analyze Cluster or SQL health issues that cause SQL Failover Cluster Instance to fail over or go offline.
           * Detect and analyze why availability group failed to failover to failover partner during manual or automatic failover attempt.

**HOW TO USE**

      1 Collect logs from the replica in the primary role when the health issue occurred using one of these tools:
              SQL Log Scout, collect Scenario 0: Available on GitHub (https://github.com/microsoft/SQL_LogScout)
              TSS, SQL Base Diagnostic collection: Available on GitHub (https://github.com/walter-1/TSS)
              PSSDiag: Available on GitHub (https://github.com/microsoft/DiagManager/releases/)

      2 Unzip the logs in a folder.

      3 Launch AGDiag and select the log folder containing the unzipped logs.


## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
